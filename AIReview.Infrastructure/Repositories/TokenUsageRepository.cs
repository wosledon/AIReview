using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Repositories;

/// <summary>
/// Token使用记录仓储实现
/// </summary>
public class TokenUsageRepository : Repository<TokenUsageRecord>, ITokenUsageRepository
{
    private readonly ILogger<TokenUsageRepository> _logger;
    
    public TokenUsageRepository(
        ApplicationDbContext context,
        ILogger<TokenUsageRepository> logger) : base(context)
    {
        _logger = logger;
    }
    
    public async Task<IEnumerable<TokenUsageRecord>> GetByUserIdAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.Where(r => r.UserId == userId);
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<TokenUsageRecord>> GetByProjectIdAsync(
        int projectId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.Where(r => r.ProjectId == projectId);
        
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<TokenUsageRecord>> GetByReviewRequestIdAsync(int reviewRequestId)
    {
        return await _dbSet
            .Where(r => r.ReviewRequestId == reviewRequestId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<TokenUsageStatistics> GetUserStatisticsAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.Where(r => r.UserId == userId);
        return await CalculateStatisticsAsync(query, startDate, endDate);
    }
    
    public async Task<TokenUsageStatistics> GetProjectStatisticsAsync(
        int projectId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.Where(r => r.ProjectId == projectId);
        return await CalculateStatisticsAsync(query, startDate, endDate);
    }
    
    public async Task<TokenUsageStatistics> GetGlobalStatisticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.AsQueryable();
        return await CalculateStatisticsAsync(query, startDate, endDate);
    }
    
    public async Task<IEnumerable<ProviderUsageStatistics>> GetProviderStatisticsAsync(
        string? userId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(r => r.UserId == userId);
            
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        // SQLite 不支持对 decimal 类型进行 Sum 聚合，这里在服务端以 double 聚合，客户端再转回 decimal
        var grouped = await query
            .GroupBy(r => new { r.Provider, r.Model })
            .Select(g => new 
            {
                g.Key.Provider,
                g.Key.Model,
                RequestCount = g.Count(),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCostDouble = g.Sum(r => (double)r.TotalCost),
                AverageTokensPerRequest = g.Average(r => r.TotalTokens)
            })
            .OrderByDescending(s => s.TotalCostDouble)
            .ToListAsync();

        return grouped.Select(g => new ProviderUsageStatistics
        {
            Provider = g.Provider,
            Model = g.Model,
            RequestCount = g.RequestCount,
            TotalTokens = g.TotalTokens,
            TotalCost = (decimal)g.TotalCostDouble,
            AverageTokensPerRequest = g.AverageTokensPerRequest
        }).ToList();
    }
    
    public async Task<IEnumerable<OperationUsageStatistics>> GetOperationStatisticsAsync(
        string? userId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(r => r.UserId == userId);
            
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        // 同上，decimal 的 Sum 用 double 聚合后再转换
        var grouped = await query
            .GroupBy(r => r.OperationType)
            .Select(g => new 
            {
                OperationType = g.Key,
                RequestCount = g.Count(),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCostDouble = g.Sum(r => (double)r.TotalCost),
                AverageTokensPerRequest = g.Average(r => r.TotalTokens)
            })
            .OrderByDescending(s => s.RequestCount)
            .ToListAsync();

        return grouped.Select(g => new OperationUsageStatistics
        {
            OperationType = g.OperationType,
            RequestCount = g.RequestCount,
            TotalTokens = g.TotalTokens,
            TotalCost = (decimal)g.TotalCostDouble,
            AverageTokensPerRequest = g.AverageTokensPerRequest
        }).ToList();
    }
    
    public async Task<IEnumerable<DailyUsageTrend>> GetDailyTrendsAsync(
        string? userId = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(r => r.UserId == userId);
            
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        
        var grouped = await query
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new 
            {
                Date = g.Key,
                RequestCount = g.Count(),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCostDouble = g.Sum(r => (double)r.TotalCost)
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        return grouped.Select(g => new DailyUsageTrend
        {
            Date = g.Date,
            RequestCount = g.RequestCount,
            TotalTokens = g.TotalTokens,
            TotalCost = (decimal)g.TotalCostDouble
        }).ToList();
    }
    
    private async Task<TokenUsageStatistics> CalculateStatisticsAsync(
        IQueryable<TokenUsageRecord> query, 
        DateTime? startDate, 
        DateTime? endDate)
    {
        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        
        var records = await query.ToListAsync();
        
        if (!records.Any())
        {
            return new TokenUsageStatistics();
        }
        
        return new TokenUsageStatistics
        {
            TotalRequests = records.Count,
            SuccessfulRequests = records.Count(r => r.IsSuccessful),
            FailedRequests = records.Count(r => !r.IsSuccessful),
            TotalPromptTokens = records.Sum(r => r.PromptTokens),
            TotalCompletionTokens = records.Sum(r => r.CompletionTokens),
            TotalTokens = records.Sum(r => r.TotalTokens),
            TotalCost = records.Sum(r => r.TotalCost),
            PromptCost = records.Sum(r => r.PromptCost),
            CompletionCost = records.Sum(r => r.CompletionCost),
            AverageResponseTimeMs = records.Where(r => r.ResponseTimeMs.HasValue).Any()
                ? records.Where(r => r.ResponseTimeMs.HasValue).Average(r => r.ResponseTimeMs ?? 0)
                : 0,
            CachedRequests = records.Count(r => r.IsCached)
        };
    }
}
