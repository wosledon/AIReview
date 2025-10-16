using Hangfire;
using Hangfire.Storage;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.BackgroundJobs;
using AIReview.Shared.DTOs;
using Microsoft.Extensions.Logging;
// Note: Avoid importing Hangfire.Storage.Monitoring to prevent type name collisions (JobDetailsDto)

namespace AIReview.Infrastructure.Services
{
    public class HangfireAsyncAnalysisService : IAsyncAnalysisService
    {
        private readonly ILogger<HangfireAsyncAnalysisService> _logger;

        public HangfireAsyncAnalysisService(ILogger<HangfireAsyncAnalysisService> logger)
        {
            _logger = logger;
        }

        public Task<string> EnqueueRiskAssessmentAsync(int reviewRequestId)
        {
            try
            {
                // 简单幂等：用分布式短锁避免瞬时重复入队
                using var _ = JobStorage.Current.GetConnection().AcquireDistributedLock($"enqueue:ai:risk:{reviewRequestId}", TimeSpan.FromMilliseconds(500));
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => job.ProcessRiskAssessmentAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued risk assessment job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return Task.FromResult(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue risk assessment for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public Task<string> EnqueueImprovementSuggestionsAsync(int reviewRequestId)
        {
            try
            {
                using var _ = JobStorage.Current.GetConnection().AcquireDistributedLock($"enqueue:ai:suggest:{reviewRequestId}", TimeSpan.FromMilliseconds(500));
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => job.ProcessImprovementSuggestionsAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued improvement suggestions job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return Task.FromResult(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue improvement suggestions for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public Task<string> EnqueuePullRequestSummaryAsync(int reviewRequestId)
        {
            try
            {
                using var _ = JobStorage.Current.GetConnection().AcquireDistributedLock($"enqueue:ai:summary:{reviewRequestId}", TimeSpan.FromMilliseconds(500));
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => job.ProcessPullRequestSummaryAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued PR summary job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return Task.FromResult(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue PR summary for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        private string? FindExistingComprehensiveJob(int reviewRequestId)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();

                // Helper local function to inspect job details safely
                bool IsTargetJob(Hangfire.Storage.Monitoring.JobDetailsDto? details)
                {
                    try
                    {
                        if (details?.Job == null) return false;
                        var job = details.Job;
                        if (job.Type != typeof(AIReview.Infrastructure.BackgroundJobs.AIAnalysisJob)) return false;
                        if (!string.Equals(job.Method?.Name, nameof(AIReview.Infrastructure.BackgroundJobs.AIAnalysisJob.ProcessComprehensiveAnalysisAsync), StringComparison.Ordinal))
                            return false;

                        // Arguments are serialized; try both typed and string compare
                        var args = job.Args ?? new List<object?>();
                        if (args.Count == 0) return false;

                        var arg0 = args[0];
                        if (arg0 is int intArg) return intArg == reviewRequestId;
                        if (arg0 is string strArg && int.TryParse(strArg, out var parsed)) return parsed == reviewRequestId;
                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                }

                // Check Enqueued jobs
                foreach (var kv in monitoring.EnqueuedJobs("ai-analysis", 0, 200))
                {
                    var jobId = kv.Key;
                    var details = monitoring.JobDetails(jobId);
                    if (IsTargetJob(details))
                    {
                        return jobId;
                    }
                }

                // Check Processing jobs
                foreach (var kv in monitoring.ProcessingJobs(0, 200))
                {
                    var jobId = kv.Key;
                    var details = monitoring.JobDetails(jobId);
                    if (IsTargetJob(details))
                    {
                        return jobId;
                    }
                }

                // Check Scheduled jobs
                foreach (var kv in monitoring.ScheduledJobs(0, 200))
                {
                    var jobId = kv.Key;
                    var details = monitoring.JobDetails(jobId);
                    if (IsTargetJob(details))
                    {
                        return jobId;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                // 如果监控API不可用，不阻塞主流程，仅记录
                _logger.LogWarning(ex, "Failed to scan existing comprehensive jobs for review {ReviewRequestId}", reviewRequestId);
                return null;
            }
        }

        public Task<string> EnqueueComprehensiveAnalysisAsync(int reviewRequestId)
        {
            try
            {
                // 使用更长的锁超时时间，确保在高并发情况下不会重复入队
                var lockKey = $"enqueue:ai:comprehensive:{reviewRequestId}";
                var lockTimeout = TimeSpan.FromSeconds(10);
                
                using var lockHandle = JobStorage.Current.GetConnection().AcquireDistributedLock(lockKey, lockTimeout);

                // 幂等：如果已有相同评审的综合分析任务在排队/执行/计划中，则直接返回已有 jobId
                var existing = FindExistingComprehensiveJob(reviewRequestId);
                if (!string.IsNullOrEmpty(existing))
                {
                    _logger.LogInformation("Comprehensive analysis already enqueued or running as {JobId} for review {ReviewRequestId}", existing, reviewRequestId);
                    return Task.FromResult(existing!);
                }

                // 入队任务，注意方法签名变化：已移除 PerformContext 参数
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => job.ProcessComprehensiveAnalysisAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued comprehensive analysis job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return Task.FromResult(jobId);
            }
            catch (DistributedLockTimeoutException ex)
            {
                _logger.LogWarning(ex, "Could not acquire lock to enqueue comprehensive analysis for review {ReviewRequestId}, likely already enqueuing", reviewRequestId);
                // 尝试查找已有任务
                var existing = FindExistingComprehensiveJob(reviewRequestId);
                if (!string.IsNullOrEmpty(existing))
                {
                    return Task.FromResult(existing!);
                }
                throw new InvalidOperationException($"Failed to enqueue comprehensive analysis for review {reviewRequestId}: concurrent enqueue detected", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue comprehensive analysis for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public Task<JobDetailsDto?> GetJobStatusAsync(string jobId)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var details = monitoring.JobDetails(jobId);
                if (details == null || details.Job == null)
                {
                    return Task.FromResult<JobDetailsDto?>(null);
                }

                var history = details.History ?? new List<Hangfire.Storage.Monitoring.StateHistoryDto>();
                var current = history.FirstOrDefault();

                return Task.FromResult<JobDetailsDto?>(new JobDetailsDto
                {
                    Id = jobId,
                    State = current?.StateName ?? "Unknown",
                    CreatedAt = details.CreatedAt,
                    StartedAt = history.FirstOrDefault(s => s.StateName == "Processing")?.CreatedAt,
                    FinishedAt = history.FirstOrDefault(s => s.StateName == "Succeeded" || s.StateName == "Failed")?.CreatedAt,
                    ErrorMessage = current?.StateName == "Failed" ? current?.Reason : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
                return Task.FromResult<JobDetailsDto?>(null);
            }
        }

        public Task<bool> CancelJobAsync(string jobId)
        {
            try
            {
                var result = BackgroundJob.Delete(jobId);
                if (result)
                {
                    _logger.LogInformation("Successfully cancelled analysis job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel analysis job {JobId}", jobId);
                }
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel analysis job {JobId}", jobId);
                return Task.FromResult(false);
            }
        }
    }
}