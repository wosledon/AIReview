using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIReview.Core.Services;

/// <summary>
/// Token使用服务接口
/// </summary>
public interface ITokenUsageService
{
    /// <summary>
    /// 记录token使用
    /// </summary>
    Task<TokenUsageRecord> RecordUsageAsync(
        string userId,
        int? projectId,
        int? reviewRequestId,
        int? llmConfigurationId,
        string provider,
        string model,
        string operationType,
        int promptTokens,
        int completionTokens,
        bool isSuccessful = true,
        string? errorMessage = null,
        int? responseTimeMs = null,
        bool isCached = false);
    
    /// <summary>
    /// 获取用户使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetUserStatisticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取项目使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetProjectStatisticsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取全局使用统计
    /// </summary>
    Task<TokenUsageStatistics> GetGlobalStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取用户的使用记录
    /// </summary>
    Task<IEnumerable<TokenUsageRecord>> GetUserRecordsAsync(string userId, int page = 1, int pageSize = 50);
    
    /// <summary>
    /// 获取项目的使用记录
    /// </summary>
    Task<IEnumerable<TokenUsageRecord>> GetProjectRecordsAsync(int projectId, int page = 1, int pageSize = 50);
    
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
    Task<IEnumerable<DailyUsageTrend>> GetDailyTrendsAsync(string? userId = null, int days = 30);
    
    /// <summary>
    /// 估算token数量(用于预计成本)
    /// </summary>
    int EstimateTokenCount(string text);
    
    /// <summary>
    /// 估算成本
    /// </summary>
    (decimal promptCost, decimal completionCost, decimal totalCost) EstimateCost(
        string provider, 
        string model, 
        int estimatedPromptTokens, 
        int estimatedCompletionTokens);
}

/// <summary>
/// Token使用服务实现
/// </summary>
public class TokenUsageService : ITokenUsageService
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly ILogger<TokenUsageService> _logger;
    
    // Token估算:粗略估计每4个字符约等于1个token(对于代码)
    private const int CHARS_PER_TOKEN = 4;
    
    public TokenUsageService(
        ITokenUsageRepository tokenUsageRepository,
        ILogger<TokenUsageService> logger)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _logger = logger;
    }
    
    public async Task<TokenUsageRecord> RecordUsageAsync(
        string userId,
        int? projectId,
        int? reviewRequestId,
        int? llmConfigurationId,
        string provider,
        string model,
        string operationType,
        int promptTokens,
        int completionTokens,
        bool isSuccessful = true,
        string? errorMessage = null,
        int? responseTimeMs = null,
        bool isCached = false)
    {
        try
        {
            // 计算成本
            var (promptCost, completionCost, totalCost) = LLMPricingService.CalculateCost(
                provider, model, promptTokens, completionTokens);
            
            var record = new TokenUsageRecord
            {
                UserId = userId,
                ProjectId = projectId,
                ReviewRequestId = reviewRequestId,
                LLMConfigurationId = llmConfigurationId,
                Provider = provider,
                Model = model,
                OperationType = operationType,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                PromptCost = promptCost,
                CompletionCost = completionCost,
                TotalCost = totalCost,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage,
                ResponseTimeMs = responseTimeMs,
                IsCached = isCached,
                CreatedAt = DateTime.UtcNow
            };
            
            await _tokenUsageRepository.AddAsync(record);
            
            _logger.LogInformation(
                "Token使用已记录: User={UserId}, Provider={Provider}, Model={Model}, " +
                "Operation={Operation}, Tokens={Tokens}, Cost=${Cost:F6}",
                userId, provider, model, operationType, record.TotalTokens, totalCost);
            
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录token使用失败");
            throw;
        }
    }
    
    public async Task<TokenUsageStatistics> GetUserStatisticsAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        return await _tokenUsageRepository.GetUserStatisticsAsync(userId, startDate, endDate);
    }
    
    public async Task<TokenUsageStatistics> GetProjectStatisticsAsync(
        int projectId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        return await _tokenUsageRepository.GetProjectStatisticsAsync(projectId, startDate, endDate);
    }
    
    public async Task<TokenUsageStatistics> GetGlobalStatisticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        return await _tokenUsageRepository.GetGlobalStatisticsAsync(startDate, endDate);
    }
    
    public async Task<IEnumerable<TokenUsageRecord>> GetUserRecordsAsync(
        string userId, 
        int page = 1, 
        int pageSize = 50)
    {
        var records = await _tokenUsageRepository.GetByUserIdAsync(userId);
        return records
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
    
    public async Task<IEnumerable<TokenUsageRecord>> GetProjectRecordsAsync(
        int projectId, 
        int page = 1, 
        int pageSize = 50)
    {
        var records = await _tokenUsageRepository.GetByProjectIdAsync(projectId);
        return records
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
    
    public async Task<IEnumerable<ProviderUsageStatistics>> GetProviderStatisticsAsync(
        string? userId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        return await _tokenUsageRepository.GetProviderStatisticsAsync(userId, startDate, endDate);
    }
    
    public async Task<IEnumerable<OperationUsageStatistics>> GetOperationStatisticsAsync(
        string? userId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        return await _tokenUsageRepository.GetOperationStatisticsAsync(userId, startDate, endDate);
    }
    
    public async Task<IEnumerable<DailyUsageTrend>> GetDailyTrendsAsync(
        string? userId = null, 
        int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var endDate = DateTime.UtcNow.Date;
        
        return await _tokenUsageRepository.GetDailyTrendsAsync(userId, startDate, endDate);
    }
    
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
            
        return text.Length / CHARS_PER_TOKEN;
    }
    
    public (decimal promptCost, decimal completionCost, decimal totalCost) EstimateCost(
        string provider, 
        string model, 
        int estimatedPromptTokens, 
        int estimatedCompletionTokens)
    {
        return LLMPricingService.CalculateCost(provider, model, estimatedPromptTokens, estimatedCompletionTokens);
    }
}
