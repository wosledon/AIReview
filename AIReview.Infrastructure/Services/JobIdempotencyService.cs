using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// Job幂等性保证服务实现
/// </summary>
public class JobIdempotencyService : IJobIdempotencyService
{
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<JobIdempotencyService> _logger;
    
    private const string ExecutionKeyPrefix = "job:execution";
    private const string LockKeyPrefix = "job:lock";

    public JobIdempotencyService(
        IDistributedCacheService cacheService,
        ILogger<JobIdempotencyService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IJobExecutionContext?> TryStartExecutionAsync(string jobType, string jobKey, TimeSpan? timeout = null)
    {
        var executionKey = BuildExecutionKey(jobType, jobKey);
        var lockKey = BuildLockKey(jobType, jobKey);
        var lockTimeout = timeout ?? TimeSpan.FromMinutes(30);

        try
        {
            // 1. 检查是否已有执行记录
            var existingStatus = await GetExecutionStatusAsync(jobType, jobKey);
            if (existingStatus != null)
            {
                if (existingStatus.Status == "executing")
                {
                    _logger.LogWarning("Job {JobType}:{JobKey} is already executing with ExecutionId: {ExecutionId}", 
                        jobType, jobKey, existingStatus.ExecutionId);
                    return null;
                }
                else if (existingStatus.Status == "completed")
                {
                    // 检查完成时间,如果在最近5分钟内完成,不允许重新执行
                    var completedRecently = existingStatus.EndTime.HasValue && 
                                          DateTime.UtcNow - existingStatus.EndTime.Value < TimeSpan.FromMinutes(5);
                    
                    if (completedRecently)
                    {
                        _logger.LogInformation("Job {JobType}:{JobKey} completed recently at {EndTime}, skipping", 
                            jobType, jobKey, existingStatus.EndTime);
                        return null;
                    }
                }
            }

            // 2. 尝试获取分布式锁
            var distributedLock = await _cacheService.TryAcquireLockAsync(lockKey, lockTimeout);
            if (distributedLock == null)
            {
                _logger.LogWarning("Failed to acquire lock for Job {JobType}:{JobKey}", jobType, jobKey);
                return null;
            }

            // 3. 再次检查执行状态(双重检查)
            existingStatus = await GetExecutionStatusAsync(jobType, jobKey);
            if (existingStatus?.Status == "executing")
            {
                _logger.LogWarning("Job {JobType}:{JobKey} is executing (double-check), releasing lock", 
                    jobType, jobKey);
                await distributedLock.DisposeAsync();
                return null;
            }

            // 4. 创建执行上下文
            var executionId = Guid.NewGuid().ToString("N");
            var context = new JobExecutionContext(
                executionId,
                jobType,
                jobKey,
                executionKey,
                _cacheService,
                distributedLock,
                _logger,
                lockTimeout);

            // 5. 记录执行状态
            var status = new JobExecutionStatus
            {
                ExecutionId = executionId,
                Status = "executing",
                StartTime = DateTime.UtcNow,
                ProgressPercentage = 0
            };

            await _cacheService.SetAsync(executionKey, status, lockTimeout + TimeSpan.FromMinutes(5));

            _logger.LogInformation("Job {JobType}:{JobKey} started execution with ExecutionId: {ExecutionId}", 
                jobType, jobKey, executionId);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting execution for Job {JobType}:{JobKey}", jobType, jobKey);
            return null;
        }
    }

    public async Task<bool> IsExecutingAsync(string jobType, string jobKey)
    {
        var status = await GetExecutionStatusAsync(jobType, jobKey);
        return status?.Status == "executing";
    }

    public async Task<JobExecutionStatus?> GetExecutionStatusAsync(string jobType, string jobKey)
    {
        var executionKey = BuildExecutionKey(jobType, jobKey);
        return await _cacheService.GetAsync<JobExecutionStatus>(executionKey);
    }

    public async Task<int> CleanupExpiredExecutionsAsync(TimeSpan olderThan)
    {
        try
        {
            var pattern = $"{ExecutionKeyPrefix}:*";
            var deletedCount = await _cacheService.RemoveByPatternAsync(pattern);
            
            _logger.LogInformation("Cleaned up {Count} expired job execution records", deletedCount);
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired executions");
            return 0;
        }
    }

    private string BuildExecutionKey(string jobType, string jobKey)
    {
        return $"{ExecutionKeyPrefix}:{jobType}:{jobKey}";
    }

    private string BuildLockKey(string jobType, string jobKey)
    {
        return $"{LockKeyPrefix}:{jobType}:{jobKey}";
    }
}

/// <summary>
/// Job执行上下文实现
/// </summary>
internal class JobExecutionContext : IJobExecutionContext
{
    private readonly string _executionKey;
    private readonly IDistributedCacheService _cacheService;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout;
    private bool _disposed;

    public string ExecutionId { get; }
    public string JobType { get; }
    public string JobKey { get; }
    public DateTime StartTime { get; }

    public JobExecutionContext(
        string executionId,
        string jobType,
        string jobKey,
        string executionKey,
        IDistributedCacheService cacheService,
        IDistributedLock distributedLock,
        ILogger logger,
        TimeSpan timeout)
    {
        ExecutionId = executionId;
        JobType = jobType;
        JobKey = jobKey;
        _executionKey = executionKey;
        _cacheService = cacheService;
        _distributedLock = distributedLock;
        _logger = logger;
        _timeout = timeout;
        StartTime = DateTime.UtcNow;
    }

    public async Task MarkSuccessAsync(object? result = null)
    {
        if (_disposed) return;

        try
        {
            var status = new JobExecutionStatus
            {
                ExecutionId = ExecutionId,
                Status = "completed",
                StartTime = StartTime,
                EndTime = DateTime.UtcNow,
                ProgressPercentage = 100,
                Result = result
            };

            // 保存完成状态,保留一段时间用于去重
            await _cacheService.SetAsync(_executionKey, status, TimeSpan.FromHours(1));

            _logger.LogInformation("Job {JobType}:{JobKey} completed successfully, ExecutionId: {ExecutionId}, Duration: {Duration}ms",
                JobType, JobKey, ExecutionId, (DateTime.UtcNow - StartTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job success for {JobType}:{JobKey}", JobType, JobKey);
        }
    }

    public async Task MarkFailureAsync(string errorMessage, Exception? exception = null)
    {
        if (_disposed) return;

        try
        {
            var status = new JobExecutionStatus
            {
                ExecutionId = ExecutionId,
                Status = "failed",
                StartTime = StartTime,
                EndTime = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };

            // 保存失败状态,保留较短时间(允许重试)
            await _cacheService.SetAsync(_executionKey, status, TimeSpan.FromMinutes(10));

            _logger.LogError(exception, "Job {JobType}:{JobKey} failed, ExecutionId: {ExecutionId}, Error: {Error}",
                JobType, JobKey, ExecutionId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job failure for {JobType}:{JobKey}", JobType, JobKey);
        }
    }

    public async Task UpdateProgressAsync(int progressPercentage, string? message = null)
    {
        if (_disposed) return;

        try
        {
            var status = await _cacheService.GetAsync<JobExecutionStatus>(_executionKey);
            if (status != null && status.Status == "executing")
            {
                status.ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
                status.ProgressMessage = message;
                
                await _cacheService.SetAsync(_executionKey, status, _timeout + TimeSpan.FromMinutes(5));

                _logger.LogDebug("Job {JobType}:{JobKey} progress: {Progress}%, message: {Message}",
                    JobType, JobKey, progressPercentage, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job progress for {JobType}:{JobKey}", JobType, JobKey);
        }
    }

    public async Task<bool> ExtendTimeoutAsync(TimeSpan additionalTime)
    {
        if (_disposed) return false;

        try
        {
            var extended = await _distributedLock.ExtendAsync(additionalTime);
            if (extended)
            {
                _logger.LogDebug("Job {JobType}:{JobKey} timeout extended by {Time}",
                    JobType, JobKey, additionalTime);
            }
            return extended;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending timeout for {JobType}:{JobKey}", JobType, JobKey);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            // 释放分布式锁
            await _distributedLock.DisposeAsync();

            _logger.LogDebug("Job execution context disposed for {JobType}:{JobKey}", JobType, JobKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing job execution context for {JobType}:{JobKey}", JobType, JobKey);
        }
    }
}
