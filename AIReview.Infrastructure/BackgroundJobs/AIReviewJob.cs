using Hangfire;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using AIReview.Shared.Enums;

namespace AIReview.Infrastructure.BackgroundJobs
{
    public class AIReviewJob
    {
        private readonly IAIReviewer _aiReviewer;
        private readonly IReviewService _reviewService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AIReviewJob> _logger;

        public AIReviewJob(
            IAIReviewer aiReviewer,
            IReviewService reviewService,
            INotificationService notificationService,
            ILogger<AIReviewJob> logger)
        {
            _aiReviewer = aiReviewer;
            _reviewService = reviewService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [Queue("ai-review")]
        public async Task ProcessReviewAsync(int reviewRequestId)
        {
            try
            {
                _logger.LogInformation("Starting AI review for request {ReviewRequestId}", reviewRequestId);

                // 验证评审是否存在
                var reviewDto = await _reviewService.GetReviewAsync(reviewRequestId);
                if (reviewDto == null)
                {
                    _logger.LogWarning("Review request {ReviewRequestId} not found", reviewRequestId);
                    return;
                }

                // 更新状态为 AIReviewing
                await _reviewService.UpdateReviewStatusAsync(reviewRequestId, ReviewState.AIReviewing);
                
                // 发送状态更新通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    reviewDto.AuthorId,
                    reviewRequestId.ToString(),
                    "ai_reviewing",
                    "AI正在分析您的代码..."
                );

                // 构造基础上下文与差异内容（此处可接入Git服务获取实际diff，暂用最简占位）
                var context = new Core.Interfaces.ReviewContext
                {
                    Language = "csharp",
                    ProjectType = "Web API",
                    CodingStandards = "Microsoft C# Coding Conventions"
                };
                var diff = $"Title: {reviewDto.Title}\nDescription: {reviewDto.Description}";

                // 执行AI评审
                var aiResult = await _aiReviewer.ReviewCodeAsync(diff, context);

                // 保存AI评审结果（包括AI生成的评论）
                await _reviewService.SaveAIReviewResultAsync(reviewRequestId, aiResult);

                // 更新状态为人工复核阶段
                await _reviewService.UpdateReviewStatusAsync(reviewRequestId, ReviewState.HumanReview);
                
                // 发送评审完成通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    reviewDto.AuthorId,
                    reviewRequestId.ToString(),
                    "human_review",
                    $"AI评审已完成，发现了 {aiResult.Comments.Count} 条建议，等待人工复核"
                );

                _logger.LogInformation("AI review completed for request {ReviewRequestId}, {CommentCount} comments generated",
                    reviewRequestId, aiResult.Comments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI review for request {ReviewRequestId}", reviewRequestId);

                // 更新状态为失败
                try
                {
                    var reviewDto = await _reviewService.GetReviewAsync(reviewRequestId);
                    await _reviewService.UpdateReviewStatusAsync(reviewRequestId, ReviewState.Rejected);
                    
                    // 发送失败通知
                    if (reviewDto != null)
                    {
                        await _notificationService.SendReviewStatusUpdateAsync(
                            reviewDto.AuthorId,
                            reviewRequestId.ToString(),
                            "failed",
                            "AI评审过程中发生错误，请重新提交或联系管理员"
                        );
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update review request status to failed for {ReviewRequestId}", reviewRequestId);
                }
            }
        }

        [Queue("ai-review")]
        public async Task ProcessBulkReviewAsync(List<int> reviewRequestIds)
        {
            _logger.LogInformation("Starting bulk AI review for {Count} requests", reviewRequestIds.Count);

            var tasks = reviewRequestIds.Select(id => ProcessReviewAsync(id));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Bulk AI review completed for {Count} requests", reviewRequestIds.Count);
        }
    }
}