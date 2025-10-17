namespace AIReview.Shared.DTOs;

/// <summary>
/// Token使用记录DTO
/// </summary>
public class TokenUsageRecordDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public int? ReviewRequestId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal PromptCost { get; set; }
    public decimal CompletionCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ResponseTimeMs { get; set; }
    public bool IsCached { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Token使用统计DTO
/// </summary>
public class TokenUsageStatisticsDto
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
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    public double CacheHitRate => TotalRequests > 0 ? (double)CachedRequests / TotalRequests * 100 : 0;
}

/// <summary>
/// 按提供商的使用统计DTO
/// </summary>
public class ProviderUsageStatisticsDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageTokensPerRequest { get; set; }
}

/// <summary>
/// 按操作类型的使用统计DTO
/// </summary>
public class OperationUsageStatisticsDto
{
    public string OperationType { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageTokensPerRequest { get; set; }
}

/// <summary>
/// 每日使用趋势DTO
/// </summary>
public class DailyUsageTrendDto
{
    public DateTime Date { get; set; }
    public long RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Token使用仪表板DTO
/// </summary>
public class TokenUsageDashboardDto
{
    public TokenUsageStatisticsDto Statistics { get; set; } = new();
    public List<ProviderUsageStatisticsDto> ProviderStatistics { get; set; } = new();
    public List<OperationUsageStatisticsDto> OperationStatistics { get; set; } = new();
    public List<DailyUsageTrendDto> DailyTrends { get; set; } = new();
    public List<TokenUsageRecordDto> RecentRecords { get; set; } = new();
}

/// <summary>
/// 成本估算请求DTO
/// </summary>
public class CostEstimateRequestDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int EstimatedCompletionTokens { get; set; } = 1000;
}

/// <summary>
/// 成本估算响应DTO
/// </summary>
public class CostEstimateResponseDto
{
    public int EstimatedPromptTokens { get; set; }
    public int EstimatedCompletionTokens { get; set; }
    public int EstimatedTotalTokens { get; set; }
    public decimal EstimatedPromptCost { get; set; }
    public decimal EstimatedCompletionCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public string Currency { get; set; } = "USD";
}
