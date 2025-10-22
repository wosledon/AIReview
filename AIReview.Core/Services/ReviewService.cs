using Microsoft.Extensions.Logging;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.Core.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectService _projectService;
    private readonly IGitService _gitService;
    private readonly IDiffParserService _diffParserService;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IProjectService projectService,
        IGitService gitService,
        IDiffParserService diffParserService,
        ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _projectService = projectService;
        _gitService = gitService;
        _diffParserService = diffParserService;
        _logger = logger;
    }

    public async Task<ReviewDto> CreateReviewAsync(CreateReviewRequest request, string authorId)
    {
        // 验证项目访问权限
        var hasAccess = await _projectService.HasProjectAccessAsync(request.ProjectId, authorId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("User does not have access to this project");

        var review = new ReviewRequest
        {
            ProjectId = request.ProjectId,
            AuthorId = authorId,
            Title = request.Title,
            Description = request.Description,
            Branch = request.Branch,
            BaseBranch = request.BaseBranch,
            PullRequestNumber = request.PullRequestNumber,
            Status = ReviewState.Pending
        };

        await _unitOfWork.ReviewRequests.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review request created: {ReviewId} by {AuthorId}", review.Id, authorId);

        return MapToReviewDto(review);
    }

    public async Task<ReviewDto?> GetReviewAsync(int id)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(id);
        if (review == null)
            return null;

        return MapToReviewDto(review);
    }

    public async Task<PagedResult<ReviewDto>> GetReviewsAsync(ReviewQueryParameters parameters)
    {
        var pagedReviews = await _unitOfWork.ReviewRequests.GetPagedReviewsAsync(parameters);

        var reviewDtos = pagedReviews.Items.Select(MapToReviewDto).ToList();

        return new PagedResult<ReviewDto>
        {
            Items = reviewDtos,
            TotalCount = pagedReviews.TotalCount,
            Page = pagedReviews.Page,
            PageSize = pagedReviews.PageSize,
            TotalPages = pagedReviews.TotalPages
        };
    }

    public async Task<ReviewDto> UpdateReviewAsync(int id, UpdateReviewRequest request)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(id);
        if (review == null)
            throw new ArgumentException($"Review with id {id} not found");

        if (!string.IsNullOrEmpty(request.Title))
            review.Title = request.Title;

        if (request.Description != null)
            review.Description = request.Description;

        if (request.Status.HasValue)
            review.Status = request.Status.Value;

        _unitOfWork.ReviewRequests.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review updated: {ReviewId}", review.Id);

        return MapToReviewDto(review);
    }

    public async Task DeleteReviewAsync(int id)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(id);
        if (review == null)
            throw new ArgumentException($"Review with id {id} not found");

        _unitOfWork.ReviewRequests.Remove(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review deleted: {ReviewId}", id);
    }

    public async Task UpdateReviewStatusAsync(int id, ReviewState status)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(id);
        if (review == null)
            throw new ArgumentException($"Review with id {id} not found");

        if (review.Status != status)
        {
            review.Status = status;
            _unitOfWork.ReviewRequests.Update(review);
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Review status updated: {ReviewId} to {Status}", id, status);
    }

    public async Task<IEnumerable<ReviewCommentDto>> GetReviewCommentsAsync(int reviewId)
    {
        var comments = await _unitOfWork.ReviewComments.GetCommentsByReviewAsync(reviewId);
        return comments.Select(MapToCommentDto);
    }

    public async Task<ReviewCommentDto> AddReviewCommentAsync(int reviewId, AddCommentRequest request, string authorId)
    {
        // 验证评审存在
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(reviewId);
        if (review == null)
            throw new ArgumentException($"Review with id {reviewId} not found");

        // 验证访问权限
        var hasAccess = await HasReviewAccessAsync(reviewId, authorId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("User does not have access to this review");

        var comment = new ReviewComment
        {
            ReviewRequestId = reviewId,
            AuthorId = authorId,
            FilePath = request.FilePath,
            LineNumber = request.LineNumber,
            Content = request.Content,
            Severity = request.Severity,
            Category = request.Category,
            Suggestion = request.Suggestion,
            IsAIGenerated = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ReviewComments.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment added to review: {CommentId} on {ReviewId}", comment.Id, reviewId);

        // 重新获取评论以包含作者信息
        var savedComment = await _unitOfWork.ReviewComments.GetByIdAsync(comment.Id);
        return MapToCommentDto(savedComment!);
    }

    public async Task<ReviewCommentDto?> GetReviewCommentAsync(int commentId)
    {
        var comment = await _unitOfWork.ReviewComments.GetByIdAsync(commentId);
        if (comment == null)
            return null;

        return MapToCommentDto(comment);
    }

    public async Task<ReviewCommentDto> UpdateReviewCommentAsync(int commentId, UpdateCommentRequest request)
    {
        var comment = await _unitOfWork.ReviewComments.GetByIdAsync(commentId);
        if (comment == null)
            throw new ArgumentException($"Comment with id {commentId} not found");

        if (!string.IsNullOrEmpty(request.Content))
            comment.Content = request.Content;

        if (request.Severity.HasValue)
            comment.Severity = request.Severity.Value;

        if (request.Category.HasValue)
            comment.Category = request.Category.Value;

        if (request.Suggestion != null)
            comment.Suggestion = request.Suggestion;

        _unitOfWork.ReviewComments.Update(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment updated: {CommentId}", commentId);

        return MapToCommentDto(comment);
    }

    public async Task DeleteReviewCommentAsync(int commentId)
    {
        var comment = await _unitOfWork.ReviewComments.GetByIdAsync(commentId);
        if (comment == null)
            throw new ArgumentException($"Comment with id {commentId} not found");

        _unitOfWork.ReviewComments.Remove(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment deleted: {CommentId}", commentId);
    }

    public async Task<ReviewDto> ApproveReviewAsync(int reviewId, string userId)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(reviewId);
        if (review == null)
            throw new ArgumentException("Review not found");

        // 检查访问权限
        var hasAccess = await HasReviewAccessAsync(reviewId, userId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("User does not have access to this review");

        // 更新状态为已通过
        review.Status = ReviewState.Approved;
        review.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ReviewRequests.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review approved: {ReviewId} by {UserId}", reviewId, userId);

        return MapToReviewDto(review);
    }

    public async Task<ReviewDto> RejectReviewAsync(int reviewId, string userId, string? reason)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(reviewId);
        if (review == null)
            throw new ArgumentException("Review not found");

        // 检查访问权限
        var hasAccess = await HasReviewAccessAsync(reviewId, userId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("User does not have access to this review");

        // 更新状态为已拒绝
        review.Status = ReviewState.Rejected;
        review.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ReviewRequests.Update(review);

        // 如果提供了拒绝原因，添加为评论
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var comment = new ReviewComment
            {
                ReviewRequestId = reviewId,
                AuthorId = userId,
                Content = $"拒绝原因: {reason}",
                Severity = ReviewCommentSeverity.Error,
                Category = ReviewCommentCategory.Quality,
                IsAIGenerated = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ReviewComments.AddAsync(comment);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Review rejected: {ReviewId} by {UserId}, reason: {Reason}", reviewId, userId, reason);

        return MapToReviewDto(review);
    }

    public async Task<AIReviewResultDto?> GetAIReviewResultAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithCommentsAsync(reviewId);
        if (review == null)
            return null;

        var aiComments = review.Comments
            .Where(c => c.IsAIGenerated)
            .Select(MapToCommentDto)
            .ToList();

        return new AIReviewResultDto
        {
            ReviewId = reviewId,
            OverallScore = review.AIOverallScore ?? 0,
            Summary = review.AISummary ?? "",
            Comments = aiComments,
            ActionableItems = new List<string>(), // 可以从AI摘要中提取
            GeneratedAt = review.AIReviewedAt ?? DateTime.MinValue
        };
    }

    public async Task SaveAIReviewResultAsync(int reviewId, AIReviewResult result)
    {
        var review = await _unitOfWork.ReviewRequests.GetByIdAsync(reviewId);
        if (review == null)
            throw new ArgumentException($"Review with id {reviewId} not found");

        // 更新评审的AI结果
        review.AIOverallScore = result.OverallScore;
        review.AISummary = result.Summary;
        review.AIReviewedAt = DateTime.UtcNow;

        // 添加AI生成的评论
        foreach (var aiComment in result.Comments)
        {
            var comment = new ReviewComment
            {
                ReviewRequestId = reviewId,
                AuthorId = review.AuthorId, // 使用评审请求的作者ID而不是系统ID
                FilePath = aiComment.FilePath,
                LineNumber = aiComment.LineNumber,
                Content = aiComment.Content,
                Severity = Enum.TryParse<ReviewCommentSeverity>(aiComment.Severity, true, out var severity) 
                    ? severity : ReviewCommentSeverity.Info,
                Category = Enum.TryParse<ReviewCommentCategory>(aiComment.Category, true, out var category) 
                    ? category : ReviewCommentCategory.Quality,
                Suggestion = aiComment.Suggestion,
                IsAIGenerated = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ReviewComments.AddAsync(comment);
        }

        _unitOfWork.ReviewRequests.Update(review);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("AI review result saved for review: {ReviewId} with {CommentCount} comments", 
            reviewId, result.Comments.Count);
    }

    public async Task<bool> HasReviewAccessAsync(int reviewId, string userId)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewId);
        if (review == null)
            return false;

        return await _projectService.HasProjectAccessAsync(review.ProjectId, userId);
    }

    public async Task<string?> GetReviewDiffAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewId);
        if (review == null)
            return null;

        try
        {
            // 获取项目关联的Git仓库
            var repositories = await _gitService.GetRepositoriesAsync(review.ProjectId);
            var repository = repositories.FirstOrDefault();
            
            if (repository == null)
            {
                _logger.LogWarning("No Git repository found for project {ProjectId}", review.ProjectId);
                return null;
            }

            // 获取代码差异
            var diff = await _gitService.GetDiffBetweenRefsAsync(repository.Id, review.BaseBranch, review.Branch);
            return diff;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff for review {ReviewId}", reviewId);
            return null;
        }
    }

    public async Task<DiffResponseDto?> GetReviewDiffDataAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewId);
        if (review == null)
            return null;

        try
        {
            // 获取项目关联的Git仓库
            var repositories = await _gitService.GetRepositoriesAsync(review.ProjectId);
            var repository = repositories.FirstOrDefault();
            
            if (repository == null)
            {
                _logger.LogWarning("No Git repository found for project {ProjectId}", review.ProjectId);
                return null;
            }

            // 获取代码差异
            var diff = await _gitService.GetDiffBetweenRefsAsync(repository.Id, review.BaseBranch, review.Branch);
            if (string.IsNullOrEmpty(diff))
                return null;

            // 解析差异
            var diffFiles = _diffParserService.ParseGitDiff(diff);

            // 获取评审评论
            var comments = await GetReviewCommentsAsync(reviewId);
            var codeComments = comments.Where(c => !string.IsNullOrEmpty(c.FilePath))
                .Select(c => new CodeCommentDto
                {
                    Id = c.Id.ToString(),
                    FilePath = c.FilePath ?? "",
                    LineNumber = c.LineNumber ?? 0,
                    Content = c.Content,
                    Author = c.AuthorName,
                    CreatedAt = c.CreatedAt,
                    Type = c.IsAIGenerated ? "ai" : "human",
                    Severity = MapSeverity(c.Severity.ToString())
                }).ToList();

            return new DiffResponseDto
            {
                Files = diffFiles,
                Comments = codeComments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff data for review {ReviewId}", reviewId);
            return null;
        }
    }

    private string MapSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "critical" => "critical",
            "error" => "error", 
            "warning" => "warning",
            _ => "info"
        };
    }

    /// <summary>
    /// 获取文件列表（轻量级，不包含diff内容）
    /// </summary>
    public async Task<DiffFileListDto?> GetReviewDiffFileListAsync(int reviewId)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewId);
        if (review == null)
            return null;

        try
        {
            // 获取项目关联的Git仓库
            var repositories = await _gitService.GetRepositoriesAsync(review.ProjectId);
            var repository = repositories.FirstOrDefault();
            
            if (repository == null)
            {
                _logger.LogWarning("No Git repository found for project {ProjectId}", review.ProjectId);
                return null;
            }

            // 获取代码差异
            var diff = await _gitService.GetDiffBetweenRefsAsync(repository.Id, review.BaseBranch, review.Branch);
            if (string.IsNullOrEmpty(diff))
                return null;

            // 解析差异 - 只提取元数据
            var diffFiles = _diffParserService.ParseGitDiff(diff);

            // 构建轻量级文件元数据列表
            var fileMetadata = diffFiles.Select(f => new DiffFileMetadataDto
            {
                OldPath = f.OldPath,
                NewPath = f.NewPath,
                Type = f.Type,
                AddedLines = f.Hunks.SelectMany(h => h.Changes).Count(c => c.Type == "insert"),
                DeletedLines = f.Hunks.SelectMany(h => h.Changes).Count(c => c.Type == "delete"),
                TotalChanges = f.Hunks.Count
            }).ToList();

            // 获取评审评论
            var comments = await GetReviewCommentsAsync(reviewId);
            var codeComments = comments.Where(c => !string.IsNullOrEmpty(c.FilePath))
                .Select(c => new CodeCommentDto
                {
                    Id = c.Id.ToString(),
                    FilePath = c.FilePath ?? "",
                    LineNumber = c.LineNumber ?? 0,
                    Content = c.Content,
                    Author = c.AuthorName,
                    CreatedAt = c.CreatedAt,
                    Type = c.IsAIGenerated ? "ai" : "human",
                    Severity = MapSeverity(c.Severity.ToString())
                }).ToList();

            return new DiffFileListDto
            {
                Files = fileMetadata,
                Comments = codeComments,
                TotalFiles = fileMetadata.Count,
                TotalAddedLines = fileMetadata.Sum(f => f.AddedLines),
                TotalDeletedLines = fileMetadata.Sum(f => f.DeletedLines)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff file list for review {ReviewId}", reviewId);
            return null;
        }
    }

    /// <summary>
    /// 获取单个文件的完整diff内容（按需加载）
    /// </summary>
    public async Task<DiffFileDetailDto?> GetReviewDiffFileAsync(int reviewId, string filePath)
    {
        var review = await _unitOfWork.ReviewRequests.GetReviewWithProjectAsync(reviewId);
        if (review == null)
            return null;

        try
        {
            // 获取项目关联的Git仓库
            var repositories = await _gitService.GetRepositoriesAsync(review.ProjectId);
            var repository = repositories.FirstOrDefault();
            
            if (repository == null)
            {
                _logger.LogWarning("No Git repository found for project {ProjectId}", review.ProjectId);
                return null;
            }

            // 获取代码差异
            var diff = await _gitService.GetDiffBetweenRefsAsync(repository.Id, review.BaseBranch, review.Branch);
            if (string.IsNullOrEmpty(diff))
                return null;

            // 解析差异并找到目标文件
            var diffFiles = _diffParserService.ParseGitDiff(diff);
            var targetFile = diffFiles.FirstOrDefault(f => 
                f.NewPath == filePath || f.OldPath == filePath);

            if (targetFile == null)
            {
                _logger.LogWarning("File {FilePath} not found in diff for review {ReviewId}", filePath, reviewId);
                return null;
            }

            // 获取该文件相关的评论
            var comments = await GetReviewCommentsAsync(reviewId);
            var fileComments = comments
                .Where(c => c.FilePath == filePath)
                .Select(c => new CodeCommentDto
                {
                    Id = c.Id.ToString(),
                    FilePath = c.FilePath ?? "",
                    LineNumber = c.LineNumber ?? 0,
                    Content = c.Content,
                    Author = c.AuthorName,
                    CreatedAt = c.CreatedAt,
                    Type = c.IsAIGenerated ? "ai" : "human",
                    Severity = MapSeverity(c.Severity.ToString())
                }).ToList();

            return new DiffFileDetailDto
            {
                File = targetFile,
                Comments = fileComments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff for file {FilePath} in review {ReviewId}", filePath, reviewId);
            return null;
        }
    }

    private ReviewDto MapToReviewDto(ReviewRequest review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            ProjectId = review.ProjectId,
            ProjectName = review.Project?.Name ?? "",
            AuthorId = review.AuthorId,
            AuthorName = review.Author?.UserName ?? "",
            Title = review.Title,
            Description = review.Description,
            Branch = review.Branch,
            BaseBranch = review.BaseBranch,
            Status = review.Status,
            PullRequestNumber = review.PullRequestNumber,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    private ReviewCommentDto MapToCommentDto(ReviewComment comment)
    {
        return new ReviewCommentDto
        {
            Id = comment.Id,
            ReviewRequestId = comment.ReviewRequestId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.Author?.DisplayName ?? comment.Author?.UserName ?? "",
            FilePath = comment.FilePath,
            LineNumber = comment.LineNumber,
            Content = comment.Content,
            Severity = comment.Severity,
            Category = comment.Category,
            IsAIGenerated = comment.IsAIGenerated,
            Suggestion = comment.Suggestion,
            CreatedAt = comment.CreatedAt
        };
    }
}