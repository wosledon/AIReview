using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using System.Security.Claims;

namespace AIReview.API.Controllers;

/// <summary>
/// 分析控制器 - 提供异步风险评估、改进建议和PR摘要分析功能
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly IAsyncAnalysisService _asyncAnalysisService;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly IImprovementSuggestionService _improvementSuggestionService;
    private readonly IPullRequestAnalysisService _pullRequestAnalysisService;
    private readonly IReviewService _reviewService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IAsyncAnalysisService asyncAnalysisService,
        IRiskAssessmentService riskAssessmentService,
        IImprovementSuggestionService improvementSuggestionService,
        IPullRequestAnalysisService pullRequestAnalysisService,
        IReviewService reviewService,
        ILogger<AnalysisController> logger)
    {
        _asyncAnalysisService = asyncAnalysisService;
        _riskAssessmentService = riskAssessmentService;
        _improvementSuggestionService = improvementSuggestionService;
        _pullRequestAnalysisService = pullRequestAnalysisService;
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// 异步生成综合分析报告（风险评估、改进建议、PR摘要）
    /// </summary>
    [HttpPost("reviews/{reviewId}/comprehensive")]
    public async Task<ActionResult<JobDetailsDto>> GenerateComprehensiveAnalysisAsync(int reviewId)
    {
        try
        {
            // 验证评审存在且用户有权限访问
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var review = await _reviewService.GetReviewAsync(reviewId);
            if (review == null)
                return NotFound($"Review with id {reviewId} not found");

            // 启动异步综合分析任务
            var jobId = await _asyncAnalysisService.EnqueueComprehensiveAnalysisAsync(reviewId);
            
            _logger.LogInformation("Enqueued comprehensive analysis job {JobId} for review {ReviewId} by user {UserId}", 
                jobId, reviewId, userId);

            return Ok(new JobDetailsDto 
            { 
                Id = jobId, 
                State = "Enqueued",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue comprehensive analysis for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while enqueuing analysis");
        }
    }

    #region 风险评估相关接口

    /// <summary>
    /// 异步生成风险评估
    /// </summary>
    [HttpPost("reviews/{reviewId}/risk-assessment")]
    public async Task<ActionResult<JobDetailsDto>> GenerateRiskAssessmentAsync(int reviewId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 启动异步风险评估任务
            var jobId = await _asyncAnalysisService.EnqueueRiskAssessmentAsync(reviewId);
            
            _logger.LogInformation("Enqueued risk assessment job {JobId} for review {ReviewId} by user {UserId}", 
                jobId, reviewId, userId);

            return Ok(new JobDetailsDto 
            { 
                Id = jobId, 
                State = "Enqueued",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue risk assessment for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while enqueuing risk assessment");
        }
    }

    /// <summary>
    /// 获取风险评估
    /// </summary>
    [HttpGet("reviews/{reviewId}/risk-assessment")]
    public async Task<ActionResult<RiskAssessmentDto>> GetRiskAssessment(int reviewId)
    {
        try
        {
            var riskAssessment = await _riskAssessmentService.GetRiskAssessmentAsync(reviewId);
            if (riskAssessment == null)
                return NotFound($"Risk assessment not found for review {reviewId}");

            return Ok(riskAssessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risk assessment for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while retrieving risk assessment");
        }
    }

    /// <summary>
    /// 更新风险评估
    /// </summary>
    [HttpPut("risk-assessments/{assessmentId}")]
    public async Task<ActionResult<RiskAssessmentDto>> UpdateRiskAssessment(int assessmentId, [FromBody] RiskAssessmentDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var updatedAssessment = await _riskAssessmentService.UpdateRiskAssessmentAsync(assessmentId, dto);
            
            _logger.LogInformation("Updated risk assessment {AssessmentId} by user {UserId}", 
                assessmentId, userId);

            return Ok(updatedAssessment);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update risk assessment {AssessmentId}", assessmentId);
            return StatusCode(500, "Internal server error occurred while updating risk assessment");
        }
    }

    #endregion

    #region 改进建议相关接口

    /// <summary>
    /// 异步生成改进建议
    /// </summary>
    [HttpPost("reviews/{reviewId}/improvement-suggestions")]
    public async Task<ActionResult<JobDetailsDto>> GenerateImprovementSuggestionsAsync(int reviewId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 启动异步改进建议任务
            var jobId = await _asyncAnalysisService.EnqueueImprovementSuggestionsAsync(reviewId);
            
            _logger.LogInformation("Enqueued improvement suggestions job {JobId} for review {ReviewId} by user {UserId}", 
                jobId, reviewId, userId);

            return Ok(new JobDetailsDto 
            { 
                Id = jobId, 
                State = "Enqueued",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue improvement suggestions for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while enqueuing improvement suggestions");
        }
    }

    /// <summary>
    /// 获取改进建议列表
    /// </summary>
    [HttpGet("reviews/{reviewId}/improvement-suggestions")]
    public async Task<ActionResult<List<ImprovementSuggestionDto>>> GetImprovementSuggestions(int reviewId)
    {
        try
        {
            var suggestions = await _improvementSuggestionService.GetImprovementSuggestionsAsync(reviewId);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get improvement suggestions for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while retrieving improvement suggestions");
        }
    }

    /// <summary>
    /// 获取单个改进建议
    /// </summary>
    [HttpGet("improvement-suggestions/{suggestionId}")]
    public async Task<ActionResult<ImprovementSuggestionDto>> GetImprovementSuggestion(int suggestionId)
    {
        try
        {
            var suggestion = await _improvementSuggestionService.GetImprovementSuggestionAsync(suggestionId);
            if (suggestion == null)
                return NotFound($"Improvement suggestion with id {suggestionId} not found");

            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get improvement suggestion {SuggestionId}", suggestionId);
            return StatusCode(500, "Internal server error occurred while retrieving improvement suggestion");
        }
    }

    /// <summary>
    /// 更新改进建议状态
    /// </summary>
    [HttpPatch("improvement-suggestions/{suggestionId}")]
    public async Task<ActionResult<ImprovementSuggestionDto>> UpdateImprovementSuggestion(
        int suggestionId, 
        [FromBody] UpdateImprovementSuggestionRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var updatedSuggestion = await _improvementSuggestionService.UpdateImprovementSuggestionAsync(suggestionId, request);
            
            _logger.LogInformation("Updated improvement suggestion {SuggestionId} by user {UserId}", 
                suggestionId, userId);

            return Ok(updatedSuggestion);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update improvement suggestion {SuggestionId}", suggestionId);
            return StatusCode(500, "Internal server error occurred while updating improvement suggestion");
        }
    }

    /// <summary>
    /// 批量更新改进建议状态
    /// </summary>
    [HttpPatch("improvement-suggestions/bulk")]
    public async Task<ActionResult<List<ImprovementSuggestionDto>>> BulkUpdateImprovementSuggestions(
        [FromBody] BulkUpdateImprovementSuggestionsRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.SuggestionIds == null || !request.SuggestionIds.Any())
                return BadRequest("SuggestionIds cannot be empty");

            var updatedSuggestions = await _improvementSuggestionService.BulkUpdateImprovementSuggestionsAsync(
                request.SuggestionIds, request.UpdateRequest);
            
            _logger.LogInformation("Bulk updated {Count} improvement suggestions by user {UserId}", 
                updatedSuggestions.Count, userId);

            return Ok(updatedSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update improvement suggestions");
            return StatusCode(500, "Internal server error occurred while bulk updating improvement suggestions");
        }
    }

    #endregion

    #region PR变更摘要相关接口

    /// <summary>
    /// 异步生成PR变更摘要
    /// </summary>
    [HttpPost("reviews/{reviewId}/change-summary")]
    public async Task<ActionResult<JobDetailsDto>> GenerateChangeSummaryAsync(int reviewId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 启动异步PR摘要任务
            var jobId = await _asyncAnalysisService.EnqueuePullRequestSummaryAsync(reviewId);
            
            _logger.LogInformation("Enqueued change summary job {JobId} for review {ReviewId} by user {UserId}", 
                jobId, reviewId, userId);

            return Ok(new JobDetailsDto 
            { 
                Id = jobId, 
                State = "Enqueued",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue change summary for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while enqueuing change summary");
        }
    }

    /// <summary>
    /// 获取PR变更摘要
    /// </summary>
    [HttpGet("reviews/{reviewId}/change-summary")]
    public async Task<ActionResult<PullRequestChangeSummaryDto>> GetChangeSummary(int reviewId)
    {
        try
        {
            var changeSummary = await _pullRequestAnalysisService.GetChangeSummaryAsync(reviewId);
            if (changeSummary == null)
                return NotFound($"Change summary not found for review {reviewId}");

            return Ok(changeSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get change summary for review {ReviewId}", reviewId);
            return StatusCode(500, "Internal server error occurred while retrieving change summary");
        }
    }

    /// <summary>
    /// 更新PR变更摘要
    /// </summary>
    [HttpPut("change-summaries/{summaryId}")]
    public async Task<ActionResult<PullRequestChangeSummaryDto>> UpdateChangeSummary(
        int summaryId, 
        [FromBody] PullRequestChangeSummaryDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var updatedSummary = await _pullRequestAnalysisService.UpdateChangeSummaryAsync(summaryId, dto);
            
            _logger.LogInformation("Updated change summary {SummaryId} by user {UserId}", 
                summaryId, userId);

            return Ok(updatedSummary);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update change summary {SummaryId}", summaryId);
            return StatusCode(500, "Internal server error occurred while updating change summary");
        }
    }

    #endregion

    #region Job Management

    [HttpGet("job/{jobId}/status")]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        try
        {
            var jobDetails = await _asyncAnalysisService.GetJobStatusAsync(jobId);
            return Ok(jobDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return StatusCode(500, "Internal server error occurred while getting job status");
        }
    }

    [HttpPost("job/{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(string jobId)
    {
        try
        {
            await _asyncAnalysisService.CancelJobAsync(jobId);
            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return StatusCode(500, "Internal server error occurred while cancelling job");
        }
    }

    #endregion
}

/// <summary>
/// 批量更新改进建议请求
/// </summary>
public class BulkUpdateImprovementSuggestionsRequest
{
    public List<int> SuggestionIds { get; set; } = new();
    public UpdateImprovementSuggestionRequest UpdateRequest { get; set; } = new();
}