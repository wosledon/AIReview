using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IRiskAssessmentRepository : IRepository<RiskAssessment>
{
    Task<RiskAssessment?> GetByReviewRequestIdAsync(int reviewRequestId);
}