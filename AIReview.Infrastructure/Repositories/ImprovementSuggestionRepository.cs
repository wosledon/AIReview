using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class ImprovementSuggestionRepository : Repository<ImprovementSuggestion>, IImprovementSuggestionRepository
{
    public ImprovementSuggestionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ImprovementSuggestion>> GetByReviewRequestIdAsync(int reviewRequestId)
    {
    return await _context.ImprovementSuggestions
            .Where(s => s.ReviewRequestId == reviewRequestId)
            .OrderBy(s => s.Priority)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ImprovementSuggestion>> GetByIdsAsync(List<int> ids)
    {
    return await _context.ImprovementSuggestions
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public async Task DeleteByReviewRequestIdAsync(int reviewRequestId)
    {
        // 使用数据库级批量删除，避免 EF 并发检查与不必要的加载
        await _context.ImprovementSuggestions
            .Where(s => s.ReviewRequestId == reviewRequestId)
            .ExecuteDeleteAsync();
    }
}