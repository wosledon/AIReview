using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IPullRequestChangeSummaryRepository : IRepository<PullRequestChangeSummary>
{
    Task<PullRequestChangeSummary?> GetByReviewRequestIdAsync(int reviewRequestId);
}