using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

/// <summary>
/// Token使用记录仓储接口
/// </summary>
public interface ITokenUsageRepository : IRepository<TokenUsageRecord>
{
    /// <summary>
    /// 获取用户的token使用记录
    /// </summary>
    Task<IEnumerable<TokenUsageRecord>> GetByUserIdAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取项目的token使用记录
    /// </summary>
    Task<IEnumerable<TokenUsageRecord>> GetByProjectIdAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取评审请求的token使用记录
    /// </summary>
    Task<IEnumerable<TokenUsageRecord>> GetByReviewRequestIdAsync(int reviewRequestId);
    
    /// <summary>
    /// 获取用户token使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetUserStatisticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取项目token使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetProjectStatisticsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取全局token使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetGlobalStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取按提供商分组的统计
    /// </summary>
    Task<IEnumerable<ProviderUsageStatistics>> GetProviderStatisticsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取按操作类型分组的统计
    /// </summary>
    Task<IEnumerable<OperationUsageStatistics>> GetOperationStatisticsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取每日使用趋势
    /// </summary>
    Task<IEnumerable<DailyUsageTrend>> GetDailyTrendsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Token使用统计信息
/// </summary>
public class TokenUsageStatistics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public long TotalPromptTokens { get; set; }
    public long TotalCompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PromptCost { get; set; }
    public decimal CompletionCost { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long CachedRequests { get; set; }
}

/// <summary>
/// 按提供商的使用统计
/// </summary>
public class ProviderUsageStatistics
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageTokensPerRequest { get; set; }
}

/// <summary>
/// 按操作类型的使用统计
/// </summary>
public class OperationUsageStatistics
{
    public string OperationType { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageTokensPerRequest { get; set; }
}

/// <summary>
/// 每日使用趋势
/// </summary>
public class DailyUsageTrend
{
    public DateTime Date { get; set; }
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
}
