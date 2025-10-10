using Hangfire;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using AIReview.Shared.Enums;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace AIReview.Infrastructure.BackgroundJobs
{
    public partial class AIReviewJob
    {
        private readonly IAIReviewer _aiReviewer;
        private readonly IReviewService _reviewService;
        private readonly INotificationService _notificationService;
        private readonly IGitService _gitService;
    private readonly IContextBuilder _contextBuilder;
        private readonly ILogger<AIReviewJob> _logger;

        public AIReviewJob(
            IAIReviewer aiReviewer,
            IReviewService reviewService,
            INotificationService notificationService,
            IGitService gitService,
            IContextBuilder contextBuilder,
            ILogger<AIReviewJob> logger)
        {
            _aiReviewer = aiReviewer;
            _reviewService = reviewService;
            _notificationService = notificationService;
            _gitService = gitService;
            _contextBuilder = contextBuilder;
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

                // 优先尝试从 Git 生成真实 diff（使用 BaseBranch...Branch 的 merge-base 差异）
                string diff = await BuildDiffAsync(reviewDto);

                // 语言自动识别 + 上下文构建
                var detectedLanguage = DetectLanguageFromDiff(diff) ?? "general";
                var baseContext = new Core.Interfaces.ReviewContext
                {
                    Language = detectedLanguage
                };
                var context = await _contextBuilder.BuildContextAsync(diff, baseContext);

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

    internal static class DiffUtils
    {
        public static string Truncate(string input, int maxChars = 200_000)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Length <= maxChars) return input;
            var head = input.Substring(0, maxChars);
            return head + "\n\n--- DIFF TRUNCATED --- (content too large for AI, truncated for review)";
        }
    }

    // 局部私有方法：根据评审信息构造 diff，优先使用 Git
    partial class AIReviewJob
    {
        private static readonly Dictionary<string, string> ExtLanguageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cs"] = "csharp",
            ["ts"] = "typescript",
            ["tsx"] = "typescript",
            ["js"] = "javascript",
            ["jsx"] = "javascript",
            ["py"] = "python",
            ["java"] = "java",
            ["go"] = "go",
            ["rb"] = "ruby",
            ["php"] = "php",
            ["rs"] = "rust",
            ["kt"] = "kotlin",
            ["kts"] = "kotlin",
            ["swift"] = "swift",
            ["cpp"] = "cpp",
            ["cxx"] = "cpp",
            ["hpp"] = "cpp",
            ["h"] = "cpp",
            ["m"] = "objective-c",
            ["mm"] = "objective-cpp",
        };

        private async Task<string> BuildDiffAsync(Shared.DTOs.ReviewDto reviewDto)
        {
            try
            {
                // 查找绑定到该项目的 Git 仓库
                var repos = await _gitService.GetRepositoriesAsync(reviewDto.ProjectId);
                var repo = repos.FirstOrDefault(r => r.IsActive) ?? repos.FirstOrDefault();

                if (repo != null)
                {
                    // 确保本地仓库可用并同步
                    await _gitService.SyncRepositoryAsync(repo.Id);

                    var baseRef = string.IsNullOrWhiteSpace(reviewDto.BaseBranch)
                        ? (repo.DefaultBranch ?? "main")
                        : reviewDto.BaseBranch;
                    var headRef = string.IsNullOrWhiteSpace(reviewDto.Branch)
                        ? (repo.DefaultBranch ?? "main")
                        : reviewDto.Branch;

                    var header = $"ReviewId: {reviewDto.Id}\nProject: {reviewDto.ProjectName}#{reviewDto.ProjectId}\nRepository: {repo.Name}\nCompare: {baseRef}...{headRef}\n\n";

                    var gitDiff = await _gitService.GetDiffBetweenRefsAsync(repo.Id, baseRef, headRef)
                                  ?? string.Empty;

                    var final = header + gitDiff;
                    return DiffUtils.Truncate(final);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build git diff for review {ReviewId}", reviewDto.Id);
            }

            // 回退：使用标题与描述占位，避免中断流程
            var fallback = $"ReviewId: {reviewDto.Id}\nProject: {reviewDto.ProjectName}#{reviewDto.ProjectId}\nTitle: {reviewDto.Title}\nDescription: {reviewDto.Description}";
            return fallback;
        }

        private string? DetectLanguageFromDiff(string diff)
        {
            if (string.IsNullOrWhiteSpace(diff)) return null;

            // 从 diff 行中抓取文件路径：优先匹配 "diff --git a/.. b/.." 或 "+++ b/.." / "--- a/.."
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            void Bump(string ext)
            {
                if (string.IsNullOrEmpty(ext)) return;
                if (ExtLanguageMap.TryGetValue(ext.Trim('.'), out var lang))
                {
                    counts[lang] = counts.TryGetValue(lang, out var c) ? c + 1 : 1;
                }
            }

            var lines = diff.Split('\n');
            var reDiff = new Regex("^diff --git a/(?<a>.+?) b/(?<b>.+)$", RegexOptions.Compiled);
            var rePlus = new Regex(@"^(\+\+\+|---) [ab]/(?<f>.+)$", RegexOptions.Compiled);

            foreach (var line in lines)
            {
                var m1 = reDiff.Match(line);
                if (m1.Success)
                {
                    var a = m1.Groups["a"].Value;
                    var b = m1.Groups["b"].Value;
                    Bump(Path.GetExtension(a));
                    Bump(Path.GetExtension(b));
                    continue;
                }

                var m2 = rePlus.Match(line);
                if (m2.Success)
                {
                    Bump(Path.GetExtension(m2.Groups["f"].Value));
                }
            }

            if (counts.Count == 0) return null;
            return counts.OrderByDescending(kv => kv.Value).First().Key;
        }
    }
}