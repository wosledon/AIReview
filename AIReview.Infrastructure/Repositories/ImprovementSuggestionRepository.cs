using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class ImprovementSuggestionRepository : Repository<ImprovementSuggestion>, IImprovementSuggestionRepository
{
    private readonly ApplicationDbContext _context;

    public ImprovementSuggestionRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
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
        var suggestions = await _context.ImprovementSuggestions
            .Where(s => s.ReviewRequestId == reviewRequestId)
            .ToListAsync();

        if (suggestions.Any())
        {
            _context.ImprovementSuggestions.RemoveRange(suggestions);
        }
    }
}