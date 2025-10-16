using Hangfire;
using Hangfire.Storage;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services
{
    public class HangfireAIReviewService : IAIReviewService
    {
        private readonly ILogger<HangfireAIReviewService> _logger;

        public HangfireAIReviewService(ILogger<HangfireAIReviewService> logger)
        {
            _logger = logger;
        }

        public Task<string> EnqueueReviewAsync(int reviewRequestId)
        {
            try
            {
                using var _ = JobStorage.Current.GetConnection().AcquireDistributedLock($"enqueue:ai:review:{reviewRequestId}", TimeSpan.FromMilliseconds(500));
                var jobId = BackgroundJob.Enqueue<AIReviewJob>(job => job.ProcessReviewAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued AI review job {JobId} for review request {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return Task.FromResult(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue AI review for request {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public Task<string> EnqueueBulkReviewAsync(List<int> reviewRequestIds)
        {
            try
            {
                var key = $"enqueue:ai:review:bulk:{string.Join('-', reviewRequestIds.OrderBy(i => i))}";
                using var _ = JobStorage.Current.GetConnection().AcquireDistributedLock(key, TimeSpan.FromMilliseconds(500));
                var jobId = BackgroundJob.Enqueue<AIReviewJob>(job => job.ProcessBulkReviewAsync(reviewRequestIds));
                
                _logger.LogInformation("Enqueued bulk AI review job {JobId} for {Count} review requests", 
                    jobId, reviewRequestIds.Count);
                
                return Task.FromResult(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue bulk AI review for {Count} requests", reviewRequestIds.Count);
                throw;
            }
        }

        public JobDetailsDto? GetJobStatus(string jobId)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var details = monitoring.JobDetails(jobId);
                if (details == null || details.Job == null)
                {
                    return null;
                }

                var history = details.History ?? new List<Hangfire.Storage.Monitoring.StateHistoryDto>();
                var current = history.FirstOrDefault();

                return new JobDetailsDto
                {
                    Id = jobId,
                    State = current?.StateName ?? "Unknown",
                    CreatedAt = details.CreatedAt,
                    StartedAt = history.FirstOrDefault(s => s.StateName == "Processing")?.CreatedAt,
                    FinishedAt = history.FirstOrDefault(s => s.StateName == "Succeeded" || s.StateName == "Failed")?.CreatedAt,
                    ErrorMessage = current?.StateName == "Failed" ? current?.Reason : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
                return null;
            }
        }

        public bool CancelJob(string jobId)
        {
            try
            {
                var result = BackgroundJob.Delete(jobId);
                if (result)
                {
                    _logger.LogInformation("Successfully cancelled job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel job {JobId}", jobId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                return false;
            }
        }
    }
}