using Hangfire;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;
using AIReview.Shared.DTOs;

namespace AIReview.Infrastructure.BackgroundJobs
{
    public class AIAnalysisJob
    {
        private readonly IRiskAssessmentService _riskAssessmentService;
        private readonly IImprovementSuggestionService _improvementSuggestionService;
        private readonly IPullRequestAnalysisService _pullRequestAnalysisService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AIAnalysisJob> _logger;

        // 用于防止重复任务的静态集合
        private static readonly HashSet<string> _processingTasks = new HashSet<string>();
        private static readonly object _lockObject = new object();

        public AIAnalysisJob(
            IRiskAssessmentService riskAssessmentService,
            IImprovementSuggestionService improvementSuggestionService,
            IPullRequestAnalysisService pullRequestAnalysisService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            ILogger<AIAnalysisJob> logger)
        {
            _riskAssessmentService = riskAssessmentService;
            _improvementSuggestionService = improvementSuggestionService;
            _pullRequestAnalysisService = pullRequestAnalysisService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string> GetUserIdFromReviewRequestAsync(int reviewRequestId)
        {
            try
            {
                var reviewRequest = await _unitOfWork.ReviewRequests.GetByIdAsync(reviewRequestId);
                return reviewRequest?.AuthorId ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user ID for review request {ReviewRequestId}", reviewRequestId);
                return "";
            }
        }

        [Queue("ai-analysis")]
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ProcessRiskAssessmentAsync(int reviewRequestId)
        {
            var taskKey = $"risk_{reviewRequestId}";
            
            lock (_lockObject)
            {
                if (_processingTasks.Contains(taskKey))
                {
                    _logger.LogWarning("Risk assessment task for review {ReviewRequestId} is already processing", reviewRequestId);
                    return;
                }
                _processingTasks.Add(taskKey);
            }

            try
            {
                _logger.LogInformation("Starting risk assessment for review {ReviewRequestId}", reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "风险评估分析已开始"
                );

                // 执行风险评估
                var result = await _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "风险评估分析已完成"
                );

                _logger.LogInformation("Risk assessment completed for review {ReviewRequestId}", reviewRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Risk assessment failed for review {ReviewRequestId}", reviewRequestId);
                
                // 获取用户ID并发送失败通知
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "失败",
                    $"风险评估分析失败: {ex.Message}"
                );
                
                throw;
            }
            finally
            {
                lock (_lockObject)
                {
                    _processingTasks.Remove(taskKey);
                }
            }
        }

        [Queue("ai-analysis")]
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ProcessImprovementSuggestionsAsync(int reviewRequestId)
        {
            var taskKey = $"suggestions_{reviewRequestId}";
            
            lock (_lockObject)
            {
                if (_processingTasks.Contains(taskKey))
                {
                    _logger.LogWarning("Improvement suggestions task for review {ReviewRequestId} is already processing", reviewRequestId);
                    return;
                }
                _processingTasks.Add(taskKey);
            }

            try
            {
                _logger.LogInformation("Starting improvement suggestions for review {ReviewRequestId}", reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "改进建议分析已开始"
                );

                // 执行改进建议分析
                var result = await _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "改进建议分析已完成"
                );

                _logger.LogInformation("Improvement suggestions completed for review {ReviewRequestId}", reviewRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Improvement suggestions failed for review {ReviewRequestId}", reviewRequestId);
                
                // 获取用户ID并发送失败通知
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "失败",
                    $"改进建议分析失败: {ex.Message}"
                );
                
                throw;
            }
            finally
            {
                lock (_lockObject)
                {
                    _processingTasks.Remove(taskKey);
                }
            }
        }

        [Queue("ai-analysis")]
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ProcessPullRequestSummaryAsync(int reviewRequestId)
        {
            var taskKey = $"summary_{reviewRequestId}";
            
            lock (_lockObject)
            {
                if (_processingTasks.Contains(taskKey))
                {
                    _logger.LogWarning("PR summary task for review {ReviewRequestId} is already processing", reviewRequestId);
                    return;
                }
                _processingTasks.Add(taskKey);
            }

            try
            {
                _logger.LogInformation("Starting PR summary for review {ReviewRequestId}", reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "PR摘要分析已开始"
                );

                // 执行PR摘要分析
                var result = await _pullRequestAnalysisService.GenerateChangeSummaryAsync(reviewRequestId);

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "PR摘要分析已完成"
                );

                _logger.LogInformation("PR summary completed for review {ReviewRequestId}", reviewRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PR summary failed for review {ReviewRequestId}", reviewRequestId);
                
                // 获取用户ID并发送失败通知
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "失败",
                    $"PR摘要分析失败: {ex.Message}"
                );
                
                throw;
            }
            finally
            {
                lock (_lockObject)
                {
                    _processingTasks.Remove(taskKey);
                }
            }
        }

        [Queue("ai-analysis")]
        [DisableConcurrentExecution(timeoutInSeconds: 600)] // 综合分析可能需要更长时间
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ProcessComprehensiveAnalysisAsync(int reviewRequestId)
        {
            var taskKey = $"comprehensive_{reviewRequestId}";
            
            lock (_lockObject)
            {
                if (_processingTasks.Contains(taskKey))
                {
                    _logger.LogWarning("Comprehensive analysis task for review {ReviewRequestId} is already processing", reviewRequestId);
                    return;
                }
                _processingTasks.Add(taskKey);
            }

            try
            {
                _logger.LogInformation("Starting comprehensive analysis for review {ReviewRequestId}", reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "综合分析已开始"
                );

                // 并行执行所有分析
                var tasks = new List<Task>
                {
                    _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId),
                    _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId),
                    _pullRequestAnalysisService.GenerateChangeSummaryAsync(reviewRequestId)
                };

                await Task.WhenAll(tasks);

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "综合分析已完成"
                );

                _logger.LogInformation("Comprehensive analysis completed for review {ReviewRequestId}", reviewRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive analysis failed for review {ReviewRequestId}", reviewRequestId);
                
                // 获取用户ID并发送失败通知
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "失败",
                    $"综合分析失败: {ex.Message}"
                );
                
                throw;
            }
            finally
            {
                lock (_lockObject)
                {
                    _processingTasks.Remove(taskKey);
                }
            }
        }
    }
}