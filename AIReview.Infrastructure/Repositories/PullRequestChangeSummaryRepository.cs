using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class PullRequestChangeSummaryRepository : Repository<PullRequestChangeSummary>, IPullRequestChangeSummaryRepository
{
    public PullRequestChangeSummaryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PullRequestChangeSummary?> GetByReviewRequestIdAsync(int reviewRequestId)
    {
        return await _context.PullRequestChangeSummaries
            .FirstOrDefaultAsync(cs => cs.ReviewRequestId == reviewRequestId);
    }
}