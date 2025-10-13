using Hangfire;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.BackgroundJobs;
using AIReview.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services
{
    public class HangfireAsyncAnalysisService : IAsyncAnalysisService
    {
        private readonly ILogger<HangfireAsyncAnalysisService> _logger;

        public HangfireAsyncAnalysisService(ILogger<HangfireAsyncAnalysisService> logger)
        {
            _logger = logger;
        }

        public async Task<string> EnqueueRiskAssessmentAsync(int reviewRequestId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => 
                    job.ProcessRiskAssessmentAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued risk assessment job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue risk assessment for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public async Task<string> EnqueueImprovementSuggestionsAsync(int reviewRequestId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => 
                    job.ProcessImprovementSuggestionsAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued improvement suggestions job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue improvement suggestions for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public async Task<string> EnqueuePullRequestSummaryAsync(int reviewRequestId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => 
                    job.ProcessPullRequestSummaryAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued PR summary job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue PR summary for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public async Task<string> EnqueueComprehensiveAnalysisAsync(int reviewRequestId)
        {
            try
            {
                var jobId = BackgroundJob.Enqueue<AIAnalysisJob>(job => 
                    job.ProcessComprehensiveAnalysisAsync(reviewRequestId));
                
                _logger.LogInformation("Enqueued comprehensive analysis job {JobId} for review {ReviewRequestId}", 
                    jobId, reviewRequestId);
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue comprehensive analysis for review {ReviewRequestId}", reviewRequestId);
                throw;
            }
        }

        public async Task<JobDetailsDto?> GetJobStatusAsync(string jobId)
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

                return await Task.FromResult(new JobDetailsDto
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
                return null;
            }
        }

        public async Task<bool> CancelJobAsync(string jobId)
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
                
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel analysis job {JobId}", jobId);
                return await Task.FromResult(false);
            }
        }
    }
}