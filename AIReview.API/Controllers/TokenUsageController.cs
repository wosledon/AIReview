using AIReview.Core.Interfaces;
using AIReview.Core.Services;
using AIReview.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIReview.API.Controllers;

/// <summary>
/// Token使用统计控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TokenUsageController : ControllerBase
{
    private readonly ITokenUsageService _tokenUsageService;
    private readonly ILogger<TokenUsageController> _logger;
    
    public TokenUsageController(
        ITokenUsageService tokenUsageService,
        ILogger<TokenUsageController> logger)
    {
        _tokenUsageService = tokenUsageService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取当前用户的token使用仪表板
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<TokenUsageDashboardDto>> GetUserDashboard(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            // 如果没有指定日期范围,使用最近N天
            if (!startDate.HasValue)
            {
                startDate = DateTime.UtcNow.AddDays(-days);
            }
            
            var dashboard = new TokenUsageDashboardDto
            {
                Statistics = MapToDto(await _tokenUsageService.GetUserStatisticsAsync(userId, startDate, endDate)),
                ProviderStatistics = (await _tokenUsageService.GetProviderStatisticsAsync(userId, startDate, endDate))
                    .Select(MapToDto).ToList(),
                OperationStatistics = (await _tokenUsageService.GetOperationStatisticsAsync(userId, startDate, endDate))
                    .Select(MapToDto).ToList(),
                DailyTrends = (await _tokenUsageService.GetDailyTrendsAsync(userId, days))
                    .Select(MapToDto).ToList(),
                RecentRecords = (await _tokenUsageService.GetUserRecordsAsync(userId, 1, 10))
                    .Select(MapToDto).ToList()
            };
            
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户token使用仪表板失败");
            return StatusCode(500, "获取仪表板数据失败");
        }
    }
    
    /// <summary>
    /// 获取项目的token使用统计
    /// </summary>
    [HttpGet("projects/{projectId}/statistics")]
    public async Task<ActionResult<TokenUsageStatisticsDto>> GetProjectStatistics(
        int projectId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var statistics = await _tokenUsageService.GetProjectStatisticsAsync(projectId, startDate, endDate);
            return Ok(MapToDto(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目token使用统计失败: ProjectId={ProjectId}", projectId);
            return StatusCode(500, "获取统计数据失败");
        }
    }
    
    /// <summary>
    /// 获取项目的token使用记录
    /// </summary>
    [HttpGet("projects/{projectId}/records")]
    public async Task<ActionResult<IEnumerable<TokenUsageRecordDto>>> GetProjectRecords(
        int projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var records = await _tokenUsageService.GetProjectRecordsAsync(projectId, page, pageSize);
            return Ok(records.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目token使用记录失败: ProjectId={ProjectId}", projectId);
            return StatusCode(500, "获取记录失败");
        }
    }
    
    /// <summary>
    /// 获取当前用户的token使用记录
    /// </summary>
    [HttpGet("records")]
    public async Task<ActionResult<IEnumerable<TokenUsageRecordDto>>> GetUserRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var records = await _tokenUsageService.GetUserRecordsAsync(userId, page, pageSize);
            return Ok(records.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户token使用记录失败");
            return StatusCode(500, "获取记录失败");
        }
    }
    
    /// <summary>
    /// 获取全局token使用统计(仅管理员)
    /// </summary>
    [HttpGet("global/statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TokenUsageStatisticsDto>> GetGlobalStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var statistics = await _tokenUsageService.GetGlobalStatisticsAsync(startDate, endDate);
            return Ok(MapToDto(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取全局token使用统计失败");
            return StatusCode(500, "获取统计数据失败");
        }
    }
    
    /// <summary>
    /// 获取按提供商分组的统计
    /// </summary>
    [HttpGet("providers/statistics")]
    public async Task<ActionResult<IEnumerable<ProviderUsageStatisticsDto>>> GetProviderStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var statistics = await _tokenUsageService.GetProviderStatisticsAsync(userId, startDate, endDate);
            return Ok(statistics.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提供商统计失败");
            return StatusCode(500, "获取统计数据失败");
        }
    }
    
    /// <summary>
    /// 获取按操作类型分组的统计
    /// </summary>
    [HttpGet("operations/statistics")]
    public async Task<ActionResult<IEnumerable<OperationUsageStatisticsDto>>> GetOperationStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var statistics = await _tokenUsageService.GetOperationStatisticsAsync(userId, startDate, endDate);
            return Ok(statistics.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作类型统计失败");
            return StatusCode(500, "获取统计数据失败");
        }
    }
    
    /// <summary>
    /// 获取每日使用趋势
    /// </summary>
    [HttpGet("trends/daily")]
    public async Task<ActionResult<IEnumerable<DailyUsageTrendDto>>> GetDailyTrends(
        [FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var trends = await _tokenUsageService.GetDailyTrendsAsync(userId, days);
            return Ok(trends.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取每日趋势失败");
            return StatusCode(500, "获取趋势数据失败");
        }
    }
    
    /// <summary>
    /// 估算token数量和成本
    /// </summary>
    [HttpPost("estimate")]
    public ActionResult<CostEstimateResponseDto> EstimateCost([FromBody] CostEstimateRequestDto request)
    {
        try
        {
            var estimatedPromptTokens = _tokenUsageService.EstimateTokenCount(request.Text);
            var estimatedCompletionTokens = request.EstimatedCompletionTokens;
            var estimatedTotalTokens = estimatedPromptTokens + estimatedCompletionTokens;
            
            var (promptCost, completionCost, totalCost) = _tokenUsageService.EstimateCost(
                request.Provider,
                request.Model,
                estimatedPromptTokens,
                estimatedCompletionTokens);
            
            return Ok(new CostEstimateResponseDto
            {
                EstimatedPromptTokens = estimatedPromptTokens,
                EstimatedCompletionTokens = estimatedCompletionTokens,
                EstimatedTotalTokens = estimatedTotalTokens,
                EstimatedPromptCost = promptCost,
                EstimatedCompletionCost = completionCost,
                EstimatedTotalCost = totalCost,
                Currency = "USD"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "估算成本失败");
            return StatusCode(500, "估算失败");
        }
    }
    
    // 映射方法
    private TokenUsageStatisticsDto MapToDto(TokenUsageStatistics stats)
    {
        return new TokenUsageStatisticsDto
        {
            TotalRequests = stats.TotalRequests,
            SuccessfulRequests = stats.SuccessfulRequests,
            FailedRequests = stats.FailedRequests,
            TotalPromptTokens = stats.TotalPromptTokens,
            TotalCompletionTokens = stats.TotalCompletionTokens,
            TotalTokens = stats.TotalTokens,
            TotalCost = stats.TotalCost,
            PromptCost = stats.PromptCost,
            CompletionCost = stats.CompletionCost,
            AverageResponseTimeMs = stats.AverageResponseTimeMs,
            CachedRequests = stats.CachedRequests
        };
    }
    
    private TokenUsageRecordDto MapToDto(Core.Entities.TokenUsageRecord record)
    {
        return new TokenUsageRecordDto
        {
            Id = record.Id,
            UserId = record.UserId,
            ProjectId = record.ProjectId,
            ReviewRequestId = record.ReviewRequestId,
            Provider = record.Provider,
            Model = record.Model,
            OperationType = record.OperationType,
            PromptTokens = record.PromptTokens,
            CompletionTokens = record.CompletionTokens,
            TotalTokens = record.TotalTokens,
            PromptCost = record.PromptCost,
            CompletionCost = record.CompletionCost,
            TotalCost = record.TotalCost,
            IsSuccessful = record.IsSuccessful,
            ErrorMessage = record.ErrorMessage,
            ResponseTimeMs = record.ResponseTimeMs,
            IsCached = record.IsCached,
            CreatedAt = record.CreatedAt
        };
    }
    
    private ProviderUsageStatisticsDto MapToDto(ProviderUsageStatistics stats)
    {
        return new ProviderUsageStatisticsDto
        {
            Provider = stats.Provider,
            Model = stats.Model,
            RequestCount = stats.RequestCount,
            TotalTokens = stats.TotalTokens,
            TotalCost = stats.TotalCost,
            AverageTokensPerRequest = stats.AverageTokensPerRequest
        };
    }
    
    private OperationUsageStatisticsDto MapToDto(OperationUsageStatistics stats)
    {
        return new OperationUsageStatisticsDto
        {
            OperationType = stats.OperationType,
            RequestCount = stats.RequestCount,
            TotalTokens = stats.TotalTokens,
            TotalCost = stats.TotalCost,
            AverageTokensPerRequest = stats.AverageTokensPerRequest
        };
    }
    
    private DailyUsageTrendDto MapToDto(DailyUsageTrend trend)
    {
        return new DailyUsageTrendDto
        {
            Date = trend.Date,
            RequestCount = trend.RequestCount,
            TotalTokens = trend.TotalTokens,
            TotalCost = trend.TotalCost
        };
    }
}
