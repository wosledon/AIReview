using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IAIReviewService _aiReviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        IReviewService reviewService, 
        IAIReviewService aiReviewService,
        ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _aiReviewService = aiReviewService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReviewDto>>>> GetReviews([FromQuery] ReviewQueryParameters parameters)
    {
        try
        {
            var reviews = await _reviewService.GetReviewsAsync(parameters);
            
            return Ok(new ApiResponse<PagedResult<ReviewDto>>
            {
                Success = true,
                Data = reviews
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews");
            return StatusCode(500, new ApiResponse<PagedResult<ReviewDto>>
            {
                Success = false,
                Message = "获取评审列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = GetCurrentUserId();
            var review = await _reviewService.CreateReviewAsync(request, userId);
            // 尝试在创建后立即入队 AI 评审（非阻塞）
            try
            {
                var jobId = await _aiReviewService.EnqueueReviewAsync(review.Id);
                _logger.LogInformation("AI review enqueued (JobId: {JobId}) for review {ReviewId}", jobId, review.Id);
            }
            catch (Exception ex)
            {
                // 入队失败不应阻止评审创建，记录警告
                _logger.LogWarning(ex, "Failed to enqueue AI review for review {ReviewId}", review.Id);
            }

            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, new ApiResponse<ReviewDto>
            {
                Success = true,
                Data = review,
                Message = "评审请求创建成功"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = "创建评审请求失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> GetReview(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var review = await _reviewService.GetReviewAsync(id);
            if (review == null)
            {
                return NotFound(new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "评审请求不存在"
                });
            }

            return Ok(new ApiResponse<ReviewDto>
            {
                Success = true,
                Data = review
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = "获取评审详情失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}/diff")]
    public async Task<ActionResult<ApiResponse<DiffResponseDto>>> GetReviewDiff(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var diffData = await _reviewService.GetReviewDiffDataAsync(id);
            if (diffData == null)
            {
                return NotFound(new ApiResponse<DiffResponseDto>
                {
                    Success = false,
                    Message = "评审代码差异不存在"
                });
            }

            return Ok(new ApiResponse<DiffResponseDto>
            {
                Success = true,
                Data = diffData,
                Message = "代码差异获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review diff for {ReviewId}", id);
            return StatusCode(500, new ApiResponse<DiffResponseDto>
            {
                Success = false,
                Message = "获取代码差异失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var review = await _reviewService.UpdateReviewAsync(id, request);
            
            return Ok(new ApiResponse<ReviewDto>
            {
                Success = true,
                Data = review,
                Message = "评审更新成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = "更新评审失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            await _reviewService.DeleteReviewAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "评审删除成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除评审失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> ApproveReview(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var review = await _reviewService.ApproveReviewAsync(id, userId);
            
            return Ok(new ApiResponse<ReviewDto>
            {
                Success = true,
                Data = review,
                Message = "评审已通过"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = "评审通过失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> RejectReview(int id, [FromBody] RejectReviewRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var review = await _reviewService.RejectReviewAsync(id, userId, request.Reason);
            
            return Ok(new ApiResponse<ReviewDto>
            {
                Success = true,
                Data = review,
                Message = "评审已拒绝"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<ReviewDto>
            {
                Success = false,
                Message = "评审拒绝失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/ai-review")]
    public async Task<ActionResult<ApiResponse<object>>> TriggerAIReview(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            await _aiReviewService.EnqueueReviewAsync(id);
            
            return Accepted(new ApiResponse<object>
            {
                Success = true,
                Message = "AI评审已启动，请稍后查看结果"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering AI review for {ReviewId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "启动AI评审失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}/ai-result")]
    public async Task<ActionResult<ApiResponse<AIReviewResultDto>>> GetAIReviewResult(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var result = await _reviewService.GetAIReviewResultAsync(id);
            if (result == null)
            {
                return NotFound(new ApiResponse<AIReviewResultDto>
                {
                    Success = false,
                    Message = "AI评审结果不存在"
                });
            }

            return Ok(new ApiResponse<AIReviewResultDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI review result for {ReviewId}", id);
            return StatusCode(500, new ApiResponse<AIReviewResultDto>
            {
                Success = false,
                Message = "获取AI评审结果失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewCommentDto>>>> GetReviewComments(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var comments = await _reviewService.GetReviewCommentsAsync(id);
            
            return Ok(new ApiResponse<IEnumerable<ReviewCommentDto>>
            {
                Success = true,
                Data = comments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review comments for {ReviewId}", id);
            return StatusCode(500, new ApiResponse<IEnumerable<ReviewCommentDto>>
            {
                Success = false,
                Message = "获取评审评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<ApiResponse<ReviewCommentDto>>> AddReviewComment(int id, [FromBody] AddCommentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ReviewCommentDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = GetCurrentUserId();
            var comment = await _reviewService.AddReviewCommentAsync(id, request, userId);
            
            return CreatedAtAction("GetReviewComment", "Comments", new { id = comment.Id }, new ApiResponse<ReviewCommentDto>
            {
                Success = true,
                Data = comment,
                Message = "评论添加成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ReviewCommentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<ReviewCommentDto>
            {
                Success = false,
                Message = "添加评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 触发增强AI分析（包含风险评估、改进建议和PR摘要）
    /// </summary>
    [HttpPost("{id}/enhanced-ai-analysis")]
    public async Task<ActionResult<ApiResponse<object>>> TriggerEnhancedAIAnalysis(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _reviewService.HasReviewAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            // 检查评审是否存在
            var review = await _reviewService.GetReviewAsync(id);
            if (review == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评审请求不存在"
                });
            }

            // 触发传统AI评审
            try
            {
                await _aiReviewService.EnqueueReviewAsync(id);
                _logger.LogInformation("Traditional AI review enqueued for review {ReviewId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue traditional AI review for review {ReviewId}", id);
            }

            return Accepted(new ApiResponse<object>
            {
                Success = true,
                Message = "增强AI分析已启动，包括风险评估、改进建议和变更摘要分析。请稍后查看结果。",
                Data = new { 
                    ReviewId = id,
                    AnalysisTypes = new[] { "risk-assessment", "improvement-suggestions", "change-summary" },
                    Message = "可通过 /api/analysis/reviews/{id}/comprehensive 获取完整分析结果"
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering enhanced AI analysis for review {ReviewId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "启动增强AI分析失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("用户未认证");
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(IReviewService reviewService, ILogger<CommentsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReviewCommentDto>>> GetReviewComment(int id)
    {
        try
        {
            var comment = await _reviewService.GetReviewCommentAsync(id);
            if (comment == null)
            {
                return NotFound(new ApiResponse<ReviewCommentDto>
                {
                    Success = false,
                    Message = "评论不存在"
                });
            }

            return Ok(new ApiResponse<ReviewCommentDto>
            {
                Success = true,
                Data = comment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", id);
            return StatusCode(500, new ApiResponse<ReviewCommentDto>
            {
                Success = false,
                Message = "获取评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ReviewCommentDto>>> UpdateComment(int id, [FromBody] UpdateCommentRequest request)
    {
        try
        {
            var comment = await _reviewService.UpdateReviewCommentAsync(id, request);
            
            return Ok(new ApiResponse<ReviewCommentDto>
            {
                Success = true,
                Data = comment,
                Message = "评论更新成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ReviewCommentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", id);
            return StatusCode(500, new ApiResponse<ReviewCommentDto>
            {
                Success = false,
                Message = "更新评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(int id)
    {
        try
        {
            await _reviewService.DeleteReviewCommentAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "评论删除成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}