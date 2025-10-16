using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class RiskAssessmentRepository : Repository<RiskAssessment>, IRiskAssessmentRepository
{
    public RiskAssessmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RiskAssessment?> GetByReviewRequestIdAsync(int reviewRequestId)
    {
        return await _context.RiskAssessments
            .FirstOrDefaultAsync(ra => ra.ReviewRequestId == reviewRequestId);
    }
}