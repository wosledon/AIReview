using Hangfire;
using Hangfire.Storage;
using Hangfire.Server;
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
        private readonly IJobIdempotencyService _jobIdempotencyService;
        private readonly ILogger<AIAnalysisJob> _logger;

        public AIAnalysisJob(
            IRiskAssessmentService riskAssessmentService,
            IImprovementSuggestionService improvementSuggestionService,
            IPullRequestAnalysisService pullRequestAnalysisService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IJobIdempotencyService jobIdempotencyService,
            ILogger<AIAnalysisJob> logger)
        {
            _riskAssessmentService = riskAssessmentService;
            _improvementSuggestionService = improvementSuggestionService;
            _pullRequestAnalysisService = pullRequestAnalysisService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _jobIdempotencyService = jobIdempotencyService;
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
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessRiskAssessmentAsync(int reviewRequestId)
        {
            // 使用幂等性服务确保Job只执行一次
            await using var executionContext = await _jobIdempotencyService.TryStartExecutionAsync(
                "risk-assessment", 
                reviewRequestId.ToString(), 
                TimeSpan.FromMinutes(30));
            
            if (executionContext == null)
            {
                _logger.LogWarning("Risk assessment for review {ReviewRequestId} is already being processed or completed recently, skipping", 
                    reviewRequestId);
                return;
            }

            try
            {
                _logger.LogInformation("[{ExecutionId}] Starting risk assessment for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "风险评估分析已开始"
                );

                await executionContext.UpdateProgressAsync(30, "正在分析风险...");

                // 执行风险评估
                var result = await _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(90, "保存分析结果");

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "风险评估分析已完成"
                );

                _logger.LogInformation("[{ExecutionId}] Risk assessment completed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkSuccessAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ExecutionId}] Risk assessment failed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkFailureAsync(ex.Message, ex);
                
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
            finally { }
        }

        [Queue("ai-analysis")]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessImprovementSuggestionsAsync(int reviewRequestId)
        {
            // 使用幂等性服务确保跨实例的任务唯一性
            await using var executionContext = await _jobIdempotencyService.TryStartExecutionAsync(
                "ai-analysis-suggestions",
                reviewRequestId.ToString(),
                TimeSpan.FromMinutes(30));

            if (executionContext == null)
            {
                _logger.LogWarning("Skip improvement suggestions for review {ReviewRequestId}: job is running or recently completed", reviewRequestId);
                return;
            }

            try
            {
                _logger.LogInformation("[{ExecutionId}] Starting improvement suggestions for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(10, "准备开始分析...");

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "改进建议分析已开始"
                );

                await executionContext.UpdateProgressAsync(30, "正在分析代码质量...");

                // 执行改进建议分析
                var result = await _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(90, "保存分析结果");

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "改进建议分析已完成"
                );

                _logger.LogInformation("[{ExecutionId}] Improvement suggestions completed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkSuccessAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ExecutionId}] Improvement suggestions failed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkFailureAsync(ex.Message, ex);
                
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
        }

        [Queue("ai-analysis")]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessPullRequestSummaryAsync(int reviewRequestId)
        {
            // 使用幂等性服务确保跨实例的任务唯一性
            await using var executionContext = await _jobIdempotencyService.TryStartExecutionAsync(
                "ai-analysis-summary",
                reviewRequestId.ToString(),
                TimeSpan.FromMinutes(30));

            if (executionContext == null)
            {
                _logger.LogWarning("Skip PR summary for review {ReviewRequestId}: job is running or recently completed", reviewRequestId);
                return;
            }

            try
            {
                _logger.LogInformation("[{ExecutionId}] Starting PR summary for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(10, "准备开始分析...");

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "PR摘要分析已开始"
                );

                await executionContext.UpdateProgressAsync(30, "正在分析PR变更...");

                // 执行PR摘要分析
                var result = await _pullRequestAnalysisService.GenerateChangeSummaryAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(90, "保存分析结果");

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "PR摘要分析已完成"
                );

                _logger.LogInformation("[{ExecutionId}] PR summary completed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkSuccessAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ExecutionId}] PR summary failed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkFailureAsync(ex.Message, ex);
                
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
        }

        /// <summary>
        /// 处理综合分析任务（风险评估 + 改进建议 + PR摘要）
        /// 使用 DisableConcurrentExecution 确保同一 reviewRequestId 的任务不会并发执行
        /// </summary>
        [Queue("ai-analysis")]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 3600)] // 1小时超时，防止同一任务并发
        public async Task ProcessComprehensiveAnalysisAsync(int reviewRequestId)
        {
            // 使用幂等性服务确保跨实例的任务唯一性
            await using var executionContext = await _jobIdempotencyService.TryStartExecutionAsync(
                "ai-analysis-comprehensive",
                reviewRequestId.ToString(),
                TimeSpan.FromHours(1));

            if (executionContext == null)
            {
                _logger.LogWarning("Skip comprehensive analysis for review {ReviewRequestId}: job is running or recently completed", reviewRequestId);
                return;
            }

            try
            {
                _logger.LogInformation("[{ExecutionId}] Starting comprehensive analysis for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);

                // 获取用户ID
                var userId = await GetUserIdFromReviewRequestAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(5, "准备开始综合分析...");

                // 发送开始通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "分析中",
                    "综合分析已开始"
                );

                // 顺序执行所有分析（避免 DbContext 并发问题）
                // 注意：不能并行执行，因为它们共享同一个 DbContext 实例
                
                await executionContext.UpdateProgressAsync(10, "开始风险评估...");
                _logger.LogInformation("[{ExecutionId}] Starting risk assessment for comprehensive analysis", 
                    executionContext.ExecutionId);
                await _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);
                
                await executionContext.UpdateProgressAsync(40, "风险评估完成，开始改进建议分析...");
                _logger.LogInformation("[{ExecutionId}] Starting improvement suggestions for comprehensive analysis", 
                    executionContext.ExecutionId);
                
                // 与独立的建议任务使用同一个Job类型，通过幂等性服务自动处理冲突
                // 如果其他实例正在执行建议分析，这里会被跳过
                await using var suggestContext = await _jobIdempotencyService.TryStartExecutionAsync(
                    "ai-analysis-suggestions",
                    reviewRequestId.ToString(),
                    TimeSpan.FromMinutes(30));
                
                if (suggestContext == null)
                {
                    _logger.LogWarning("[{ExecutionId}] Skip improvement suggestions inside comprehensive for review {ReviewRequestId}: another suggestions instance is running or recently completed", 
                        executionContext.ExecutionId, reviewRequestId);
                }
                else
                {
                    await suggestContext.UpdateProgressAsync(30, "正在分析代码质量...");
                    await _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);
                    await suggestContext.MarkSuccessAsync(null);
                }
                
                await executionContext.UpdateProgressAsync(70, "改进建议完成，开始PR摘要分析...");
                _logger.LogInformation("[{ExecutionId}] Starting PR summary for comprehensive analysis", 
                    executionContext.ExecutionId);
                await _pullRequestAnalysisService.GenerateChangeSummaryAsync(reviewRequestId);

                await executionContext.UpdateProgressAsync(95, "所有分析完成，保存结果...");

                // 发送完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    userId,
                    reviewRequestId.ToString(),
                    "已完成",
                    "综合分析已完成"
                );

                _logger.LogInformation("[{ExecutionId}] Comprehensive analysis completed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkSuccessAsync(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ExecutionId}] Comprehensive analysis failed for review {ReviewRequestId}", 
                    executionContext.ExecutionId, reviewRequestId);
                
                await executionContext.MarkFailureAsync(ex.Message, ex);
                
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
        }
    }
}