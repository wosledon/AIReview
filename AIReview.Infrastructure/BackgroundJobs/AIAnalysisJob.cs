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
        private readonly ILogger<AIAnalysisJob> _logger;

        // 通过 Hangfire 分布式锁来防止相同 ReviewId 的同类任务并发执行（支持分布式）
        private IDisposable? TryAcquireDistributedLock(string resource, TimeSpan timeout)
        {
            try
            {
                var conn = JobStorage.Current.GetConnection();
                return conn.AcquireDistributedLock(resource, timeout);
            }
            catch (DistributedLockTimeoutException)
            {
                return null;
            }
        }

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
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessRiskAssessmentAsync(int reviewRequestId)
        {
            // 分布式互斥：同一评审的风险评估只允许一个在执行
            var lockKey = $"lock:ai:analysis:risk:{reviewRequestId}";
            using var distLock = TryAcquireDistributedLock(lockKey, TimeSpan.FromSeconds(1));
            if (distLock == null)
            {
                _logger.LogWarning("Skip risk assessment for review {ReviewRequestId}: another instance is running", reviewRequestId);
                return;
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
            finally { }
        }

        [Queue("ai-analysis")]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessImprovementSuggestionsAsync(int reviewRequestId)
        {
            var lockKey = $"lock:ai:analysis:suggestions:{reviewRequestId}";
            using var distLock = TryAcquireDistributedLock(lockKey, TimeSpan.FromSeconds(1));
            if (distLock == null)
            {
                _logger.LogWarning("Skip improvement suggestions for review {ReviewRequestId}: another instance is running", reviewRequestId);
                return;
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
            finally { }
        }

        [Queue("ai-analysis")]
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [DisableConcurrentExecution(timeoutInSeconds: 1800)] // 30分钟超时
        public async Task ProcessPullRequestSummaryAsync(int reviewRequestId)
        {
            var lockKey = $"lock:ai:analysis:summary:{reviewRequestId}";
            using var distLock = TryAcquireDistributedLock(lockKey, TimeSpan.FromSeconds(1));
            if (distLock == null)
            {
                _logger.LogWarning("Skip PR summary for review {ReviewRequestId}: another instance is running", reviewRequestId);
                return;
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
            finally { }
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
            var lockKey = $"lock:ai:analysis:comprehensive:{reviewRequestId}";
            using var distLock = TryAcquireDistributedLock(lockKey, TimeSpan.FromSeconds(2));
            if (distLock == null)
            {
                _logger.LogWarning("Skip comprehensive analysis for review {ReviewRequestId}: another instance is running", reviewRequestId);
                return;
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

                // 顺序执行所有分析（避免 DbContext 并发问题）
                // 注意：不能并行执行，因为它们共享同一个 DbContext 实例
                _logger.LogInformation("Starting risk assessment for comprehensive analysis");
                await _riskAssessmentService.GenerateRiskAssessmentAsync(reviewRequestId);
                
                _logger.LogInformation("Starting improvement suggestions for comprehensive analysis");
                // 与独立的建议任务使用同一把分布式锁，避免并发导致的删除/插入冲突
                var suggestLockKey = $"lock:ai:analysis:suggestions:{reviewRequestId}";
                using (var suggestLock = TryAcquireDistributedLock(suggestLockKey, TimeSpan.FromSeconds(2)))
                {
                    if (suggestLock == null)
                    {
                        _logger.LogWarning("Skip improvement suggestions inside comprehensive for review {ReviewRequestId}: another suggestions instance is running", reviewRequestId);
                    }
                    else
                    {
                        await _improvementSuggestionService.GenerateImprovementSuggestionsAsync(reviewRequestId);
                    }
                }
                
                _logger.LogInformation("Starting PR summary for comprehensive analysis");
                await _pullRequestAnalysisService.GenerateChangeSummaryAsync(reviewRequestId);

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
            finally { }
        }
    }
}