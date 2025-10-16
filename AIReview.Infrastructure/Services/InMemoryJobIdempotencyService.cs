using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// 内存版Job幂等性服务(降级方案)
/// 注意: 此实现仅适用于单实例部署,不支持真正的分布式场景
/// </summary>
public class InMemoryJobIdempotencyService : IJobIdempotencyService
{
    private readonly ILogger<InMemoryJobIdempotencyService> _logger;
    private static readonly ConcurrentDictionary<string, JobExecutionStatus> _executionStates = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public InMemoryJobIdempotencyService(ILogger<InMemoryJobIdempotencyService> logger)
    {
        _logger = logger;
    }

    public async Task<IJobExecutionContext?> TryStartExecutionAsync(
        string jobType,
        string jobKey,
        TimeSpan? timeout = null)
    {
        var lockKey = $"{jobType}:{jobKey}";
        var executionTimeout = timeout ?? TimeSpan.FromMinutes(30);

        // 快速检查: 如果Job正在执行或最近完成,直接返回null
        if (_executionStates.TryGetValue(lockKey, out var existingState))
        {
            if (existingState.Status == "executing")
            {
                _logger.LogWarning("Job {JobType}:{JobKey} is already executing with ExecutionId {ExecutionId}",
                    jobType, jobKey, existingState.ExecutionId);
                return null;
            }

            if (existingState.Status == "completed" &&
                existingState.EndTime.HasValue &&
                DateTime.UtcNow - existingState.EndTime.Value < TimeSpan.FromMinutes(5))
            {
                _logger.LogWarning("Job {JobType}:{JobKey} completed recently at {CompletedAt}, skipping re-execution",
                    jobType, jobKey, existingState.EndTime);
                return null;
            }
        }

        // 获取锁
        var semaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));

        if (!acquired)
        {
            _logger.LogWarning("Failed to acquire lock for Job {JobType}:{JobKey}", jobType, jobKey);
            return null;
        }

        try
        {
            // 双重检查: 获取锁后再次验证状态
            if (_executionStates.TryGetValue(lockKey, out var stateAfterLock))
            {
                if (stateAfterLock.Status == "executing")
                {
                    _logger.LogWarning("Job {JobType}:{JobKey} is executing (double-check after lock)",
                        jobType, jobKey);
                    semaphore.Release();
                    return null;
                }

                if (stateAfterLock.Status == "completed" &&
                    stateAfterLock.EndTime.HasValue &&
                    DateTime.UtcNow - stateAfterLock.EndTime.Value < TimeSpan.FromMinutes(5))
                {
                    _logger.LogWarning("Job {JobType}:{JobKey} completed recently (double-check after lock)",
                        jobType, jobKey);
                    semaphore.Release();
                    return null;
                }
            }

            // 创建执行上下文
            var executionId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;
            var state = new JobExecutionStatus
            {
                ExecutionId = executionId,
                Status = "executing",
                StartTime = startTime,
                ProgressPercentage = 0,
                ProgressMessage = "初始化中..."
            };

            _executionStates[lockKey] = state;

            _logger.LogInformation("Job {JobType}:{JobKey} started with ExecutionId {ExecutionId}",
                jobType, jobKey, executionId);

            return new InMemoryJobExecutionContext(
                lockKey,
                jobType,
                jobKey,
                executionId,
                startTime,
                semaphore,
                this,
                _logger);
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    public async Task<bool> IsExecutingAsync(string jobType, string jobKey)
    {
        var lockKey = $"{jobType}:{jobKey}";
        if (_executionStates.TryGetValue(lockKey, out var state))
        {
            return state.Status == "executing";
        }
        return false;
    }

    public async Task<JobExecutionStatus?> GetExecutionStatusAsync(string jobType, string jobKey)
    {
        var lockKey = $"{jobType}:{jobKey}";
        if (_executionStates.TryGetValue(lockKey, out var state))
        {
            return state;
        }
        return null;
    }

    public async Task<int> CleanupExpiredExecutionsAsync(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var keysToRemove = new List<string>();

        foreach (var kvp in _executionStates)
        {
            if (kvp.Value.StartTime < cutoffTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _executionStates.TryRemove(key, out _);
            _locks.TryRemove(key, out _);
        }

        _logger.LogInformation("Cleaned up {Count} expired job execution records", keysToRemove.Count);
        return keysToRemove.Count;
    }

    internal async Task UpdateProgressAsync(string lockKey, int percentage, string? message)
    {
        if (_executionStates.TryGetValue(lockKey, out var state))
        {
            state.ProgressPercentage = percentage;
            state.ProgressMessage = message;
            _logger.LogDebug("Job {LockKey} progress: {Percentage}% - {Message}",
                lockKey, percentage, message);
        }
    }

    internal async Task MarkSuccessAsync(string lockKey, object? result)
    {
        if (_executionStates.TryGetValue(lockKey, out var state))
        {
            state.Status = "completed";
            state.EndTime = DateTime.UtcNow;
            state.ProgressPercentage = 100;
            state.ProgressMessage = "执行成功";
            state.Result = result;
            _logger.LogInformation("Job {LockKey} completed successfully with ExecutionId {ExecutionId}",
                lockKey, state.ExecutionId);
        }
    }

    internal async Task MarkFailureAsync(string lockKey, string errorMessage, Exception? exception)
    {
        if (_executionStates.TryGetValue(lockKey, out var state))
        {
            state.Status = "failed";
            state.EndTime = DateTime.UtcNow;
            state.ProgressMessage = $"执行失败: {errorMessage}";
            state.ErrorMessage = errorMessage;
            _logger.LogError(exception, "Job {LockKey} failed with ExecutionId {ExecutionId}: {ErrorMessage}",
                lockKey, state.ExecutionId, errorMessage);
        }
    }

    private class InMemoryJobExecutionContext : IJobExecutionContext
    {
        private readonly string _lockKey;
        private readonly SemaphoreSlim _semaphore;
        private readonly InMemoryJobIdempotencyService _service;
        private readonly ILogger _logger;
        private bool _disposed;

        public string ExecutionId { get; }
        public string JobType { get; }
        public string JobKey { get; }
        public DateTime StartTime { get; }

        public InMemoryJobExecutionContext(
            string lockKey,
            string jobType,
            string jobKey,
            string executionId,
            DateTime startTime,
            SemaphoreSlim semaphore,
            InMemoryJobIdempotencyService service,
            ILogger logger)
        {
            _lockKey = lockKey;
            JobType = jobType;
            JobKey = jobKey;
            ExecutionId = executionId;
            StartTime = startTime;
            _semaphore = semaphore;
            _service = service;
            _logger = logger;
        }

        public async Task UpdateProgressAsync(int progressPercentage, string? message = null)
        {
            await _service.UpdateProgressAsync(_lockKey, progressPercentage, message);
        }

        public async Task MarkSuccessAsync(object? result = null)
        {
            await _service.MarkSuccessAsync(_lockKey, result);
        }

        public async Task MarkFailureAsync(string errorMessage, Exception? exception = null)
        {
            await _service.MarkFailureAsync(_lockKey, errorMessage, exception);
        }

        public async Task<bool> ExtendTimeoutAsync(TimeSpan additionalTime)
        {
            // 内存版本不需要延长超时,总是返回true
            _logger.LogDebug("ExtendTimeoutAsync called for {LockKey}, but in-memory implementation doesn't need timeout extension", _lockKey);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    _semaphore.Release();
                    _logger.LogDebug("Released lock for {LockKey} with ExecutionId {ExecutionId}",
                        _lockKey, ExecutionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error releasing lock for {LockKey}", _lockKey);
                }
                _disposed = true;
            }
        }
    }
}
