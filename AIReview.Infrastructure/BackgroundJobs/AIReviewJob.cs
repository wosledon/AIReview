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
        private readonly IMultiLLMService _multiLLMService;
        private readonly Core.Services.ProjectGitMigrationService _projectGitMigrationService;

        // 用于防止重复任务的静态集合
        private static readonly HashSet<int> _processingTasks = new HashSet<int>();
        private static readonly object _lockObject = new object();
        private readonly ILogger<AIReviewJob> _logger;

        public AIReviewJob(
            IAIReviewer aiReviewer,
            IReviewService reviewService,
            INotificationService notificationService,
            IGitService gitService,
            IContextBuilder contextBuilder,
            IMultiLLMService multiLLMService,
            Core.Services.ProjectGitMigrationService projectGitMigrationService,
            ILogger<AIReviewJob> logger)
        {
            _aiReviewer = aiReviewer;
            _reviewService = reviewService;
            _notificationService = notificationService;
            _gitService = gitService;
            _contextBuilder = contextBuilder;
            _multiLLMService = multiLLMService;
            _projectGitMigrationService = projectGitMigrationService;
            _logger = logger;
        }

        [Queue("ai-review")]
        [DisableConcurrentExecution(timeoutInSeconds: 300)] // 防止同一个任务并发执行
        [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ProcessReviewAsync(int reviewRequestId)
        {
            var startTime = DateTime.UtcNow;
            var executionId = Guid.NewGuid().ToString("N")[..8]; // 生成执行ID用于跟踪
            
            // 防止重复处理相同的任务
            lock (_lockObject)
            {
                if (_processingTasks.Contains(reviewRequestId))
                {
                    _logger.LogWarning("[{ExecutionId}] Review request {ReviewRequestId} is already being processed by another instance",
                        executionId, reviewRequestId);
                    return;
                }
                _processingTasks.Add(reviewRequestId);
            }
            
            try
            {
                _logger.LogInformation("[{ExecutionId}] Starting AI review for request {ReviewRequestId}", 
                    executionId, reviewRequestId);

                // 验证评审是否存在
                var reviewDto = await _reviewService.GetReviewAsync(reviewRequestId);
                if (reviewDto == null)
                {
                    _logger.LogWarning("[{ExecutionId}] Review request {ReviewRequestId} not found", 
                        executionId, reviewRequestId);
                    return;
                }

                // 检查评审状态，防止重复处理
                if (reviewDto.Status != ReviewState.Pending)
                {
                    _logger.LogWarning("[{ExecutionId}] Review request {ReviewRequestId} is already in {Status} state, skipping processing", 
                        executionId, reviewRequestId, reviewDto.Status);
                    return;
                }

                // 检查是否有可用的LLM配置
                var availableConfigs = await _multiLLMService.GetAvailableConfigurationsAsync();
                if (!availableConfigs.Any())
                {
                    _logger.LogError("[{ExecutionId}] No active LLM configurations available for review {ReviewRequestId}", 
                        executionId, reviewRequestId);
                    await _reviewService.UpdateReviewStatusAsync(reviewRequestId, ReviewState.Rejected);
                    await _notificationService.SendReviewStatusUpdateAsync(
                        reviewDto.AuthorId,
                        reviewRequestId.ToString(),
                        "failed",
                        "没有可用的LLM配置，请联系管理员配置AI服务"
                    );
                    return;
                }

                _logger.LogInformation("[{ExecutionId}] Found {ConfigCount} available LLM configurations for review {ReviewRequestId}", 
                    executionId, availableConfigs.Count(), reviewRequestId);

                // 原子性地更新状态为 AIReviewing（防止并发处理）
                var statusUpdated = await TryUpdateReviewStatusAsync(reviewRequestId, ReviewState.Pending, ReviewState.AIReviewing);
                if (!statusUpdated)
                {
                    _logger.LogWarning("[{ExecutionId}] Review {ReviewRequestId} status update failed, another process may be handling it", 
                        executionId, reviewRequestId);
                    return;
                }
                
                // 发送状态更新通知
                await _notificationService.SendReviewStatusUpdateAsync(
                    reviewDto.AuthorId,
                    reviewRequestId.ToString(),
                    "ai_reviewing",
                    "AI正在分析您的代码..."
                );

                // 构建代码差异（优先使用Git，回退到基本信息）
                string diff = await BuildDiffAsync(reviewDto);

                // 语言自动识别 + 上下文构建
                var detectedLanguage = DetectLanguageFromDiff(diff) ?? "general";
                _logger.LogInformation("[{ExecutionId}] Detected language: {Language} for review {ReviewRequestId}", 
                    executionId, detectedLanguage, reviewRequestId);

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

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("[{ExecutionId}] AI review completed for request {ReviewRequestId}, {CommentCount} comments generated, duration: {Duration}ms",
                    executionId, reviewRequestId, aiResult.Comments.Count, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[{ExecutionId}] Error processing AI review for request {ReviewRequestId}, duration: {Duration}ms", 
                    executionId, reviewRequestId, duration.TotalMilliseconds);

                // 更新状态为失败
                try
                {
                    var reviewDto = await _reviewService.GetReviewAsync(reviewRequestId);
                    await _reviewService.UpdateReviewStatusAsync(reviewRequestId, ReviewState.Rejected);
                    
                    // 发送失败通知
                    if (reviewDto != null)
                    {
                        var errorMessage = ex.InnerException?.Message ?? ex.Message;
                        await _notificationService.SendReviewStatusUpdateAsync(
                            reviewDto.AuthorId,
                            reviewRequestId.ToString(),
                            "failed",
                            $"AI评审过程中发生错误: {errorMessage}，请重新提交或联系管理员"
                        );
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "[{ExecutionId}] Failed to update review request status to failed for {ReviewRequestId}", 
                        executionId, reviewRequestId);
                }
            }
            finally
            {
                // 清除处理中的任务标记
                lock (_lockObject)
                {
                    _processingTasks.Remove(reviewRequestId);
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

        private async Task<string> BuildDiffAsync(AIReview.Shared.DTOs.ReviewDto reviewDto)
        {
            try
            {
                _logger.LogInformation("Building diff for review {ReviewId} in project {ProjectId}", reviewDto.Id, reviewDto.ProjectId);
                
                // 查找绑定到该项目的 Git 仓库
                var repos = await _gitService.GetRepositoriesAsync(reviewDto.ProjectId);
                var repo = repos.FirstOrDefault(r => r.IsActive) ?? repos.FirstOrDefault();

                _logger.LogInformation("Found {RepoCount} repositories for project {ProjectId}", repos.Count(), reviewDto.ProjectId);

                // 如果没有找到Git仓库，尝试从项目信息创建临时仓库记录
                if (repo == null)
                {
                    _logger.LogWarning("No Git repository found for project {ProjectId}, checking project repository URL", reviewDto.ProjectId);
                    repo = await TryCreateRepositoryFromProjectAsync(reviewDto.ProjectId);
                }

                if (repo == null)
                {
                    _logger.LogWarning("No Git repository found for project {ProjectId}", reviewDto.ProjectId);
                    return BuildFallbackDiff(reviewDto, "未找到关联的Git仓库");
                }

                _logger.LogInformation("Using Git repository {RepoName} (ID: {RepoId}) for review {ReviewId}", 
                    repo.Name, repo.Id, reviewDto.Id);

                // 检查仓库是否可访问
                var isAccessible = await _gitService.IsRepositoryAccessibleAsync(repo.Id);
                _logger.LogInformation("Repository {RepoId} accessibility check: {IsAccessible}", repo.Id, isAccessible);
                
                if (!isAccessible)
                {
                    _logger.LogWarning("Git repository {RepoId} is not accessible", repo.Id);
                    return BuildFallbackDiff(reviewDto, "Git仓库不可访问");
                }

                // 确保本地仓库可用并同步
                _logger.LogInformation("Attempting to sync repository {RepoId}", repo.Id);
                var syncSuccess = await _gitService.SyncRepositoryAsync(repo.Id);
                _logger.LogInformation("Repository {RepoId} sync result: {SyncSuccess}", repo.Id, syncSuccess);
                
                if (!syncSuccess)
                {
                    _logger.LogWarning("Failed to sync Git repository {RepoId}", repo.Id);
                    return BuildFallbackDiff(reviewDto, "Git仓库同步失败");
                }

                var baseRef = string.IsNullOrWhiteSpace(reviewDto.BaseBranch)
                    ? (repo.DefaultBranch ?? "main")
                    : reviewDto.BaseBranch;
                var headRef = string.IsNullOrWhiteSpace(reviewDto.Branch)
                    ? (repo.DefaultBranch ?? "main")
                    : reviewDto.Branch;

                _logger.LogInformation("Generating diff between {BaseRef} and {HeadRef} for repository {RepoId}", 
                    baseRef, headRef, repo.Id);

                var header = $"ReviewId: {reviewDto.Id}\nProject: {reviewDto.ProjectName}#{reviewDto.ProjectId}\nRepository: {repo.Name}\nCompare: {baseRef}...{headRef}\n\n";

                var gitDiff = await _gitService.GetDiffBetweenRefsAsync(repo.Id, baseRef, headRef);
                
                _logger.LogInformation("Git diff result: Length={DiffLength}, IsEmpty={IsEmpty}", 
                    gitDiff?.Length ?? 0, string.IsNullOrEmpty(gitDiff));
                
                if (string.IsNullOrEmpty(gitDiff))
                {
                    _logger.LogWarning("No diff found between {BaseRef} and {HeadRef} in repository {RepoId}", 
                        baseRef, headRef, repo.Id);
                    
                    // 如果是相同分支，尝试获取最新提交的差异
                    if (baseRef == headRef)
                    {
                        _logger.LogInformation("Same branch detected, attempting to get latest commit diff");
                        var latestCommitSha = await _gitService.GetLatestCommitShaAsync(repo.Id, headRef);
                        if (!string.IsNullOrEmpty(latestCommitSha))
                        {
                            gitDiff = await _gitService.GetCommitDiffAsync(repo.Id, latestCommitSha);
                            _logger.LogInformation("Latest commit diff result: Length={DiffLength}", gitDiff?.Length ?? 0);
                        }
                    }
                    
                    if (string.IsNullOrEmpty(gitDiff))
                    {
                        return BuildFallbackDiff(reviewDto, "未找到代码差异 - 可能是分支相同或没有变更");
                    }
                }

                var final = header + gitDiff;
                var truncatedDiff = DiffUtils.Truncate(final);
                
                _logger.LogInformation("Successfully built diff for review {ReviewId}, original size: {OriginalSize}, truncated size: {TruncatedSize}", 
                    reviewDto.Id, final.Length, truncatedDiff.Length);
                
                // 记录diff的前200个字符以便调试
                var preview = final.Length > 200 ? final.Substring(0, 200) + "..." : final;
                _logger.LogDebug("Diff preview for review {ReviewId}: {DiffPreview}", reviewDto.Id, preview);
                
                return truncatedDiff;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build git diff for review {ReviewId}", reviewDto.Id);
                return BuildFallbackDiff(reviewDto, $"Git操作失败: {ex.Message}");
            }
        }

        private async Task<Core.Entities.GitRepository?> TryCreateRepositoryFromProjectAsync(int projectId)
        {
            try
            {
                _logger.LogInformation("Attempting to create Git repository for project {ProjectId}", projectId);
                
                // 使用迁移服务尝试为项目创建Git仓库记录
                var success = await _projectGitMigrationService.EnsureProjectHasGitRepositoryAsync(projectId);
                
                if (success)
                {
                    // 重新查询Git仓库
                    var repos = await _gitService.GetRepositoriesAsync(projectId);
                    var repo = repos.FirstOrDefault(r => r.IsActive) ?? repos.FirstOrDefault();
                    
                    if (repo != null)
                    {
                        _logger.LogInformation("Successfully created and retrieved Git repository {RepoId} for project {ProjectId}", 
                            repo.Id, projectId);
                        return repo;
                    }
                }
                
                _logger.LogWarning("Failed to create Git repository for project {ProjectId}", projectId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create repository from project {ProjectId}", projectId);
                return null;
            }
        }

        /// <summary>
        /// 尝试原子性地更新评审状态，防止并发处理
        /// </summary>
        private async Task<bool> TryUpdateReviewStatusAsync(int reviewRequestId, ReviewState expectedCurrentStatus, ReviewState newStatus)
        {
            try
            {
                // 重新获取当前评审状态
                var currentReview = await _reviewService.GetReviewAsync(reviewRequestId);
                if (currentReview == null)
                {
                    _logger.LogWarning("Review {ReviewRequestId} not found during status update", reviewRequestId);
                    return false;
                }

                // 检查当前状态是否符合预期
                if (currentReview.Status != expectedCurrentStatus)
                {
                    _logger.LogWarning("Review {ReviewRequestId} status is {CurrentStatus}, expected {ExpectedStatus}", 
                        reviewRequestId, currentReview.Status, expectedCurrentStatus);
                    return false;
                }

                // 执行状态更新
                await _reviewService.UpdateReviewStatusAsync(reviewRequestId, newStatus);
                _logger.LogDebug("Successfully updated review {ReviewRequestId} status from {OldStatus} to {NewStatus}", 
                    reviewRequestId, expectedCurrentStatus, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update review {ReviewRequestId} status from {OldStatus} to {NewStatus}", 
                    reviewRequestId, expectedCurrentStatus, newStatus);
                return false;
            }
        }

        private string BuildFallbackDiff(AIReview.Shared.DTOs.ReviewDto reviewDto, string reason)
        {
            _logger.LogInformation("Using fallback diff for review {ReviewId}, reason: {Reason}", reviewDto.Id, reason);
            
            var fallback = $@"ReviewId: {reviewDto.Id}
Project: {reviewDto.ProjectName}#{reviewDto.ProjectId}
Title: {reviewDto.Title}
Description: {reviewDto.Description ?? "无描述"}
Branch: {reviewDto.Branch}
BaseBranch: {reviewDto.BaseBranch}

注意: {reason}

基于项目信息的代码审查:
- 项目语言: C# (.NET)
- 评审范围: 项目整体代码质量
- 建议重点: 架构设计、代码规范、安全性

请从以下方面进行评审:
1. 整体架构设计合理性
2. 代码规范和最佳实践遵循情况  
3. 潜在的安全风险
4. 性能优化建议
5. 可维护性和可扩展性

由于无法获取具体的代码差异，建议：
1. 检查项目的Git仓库配置是否正确
2. 确认仓库URL可访问且有相应权限
3. 验证分支名称是否正确
4. 检查网络连接和Git服务状态";
            
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