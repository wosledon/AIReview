using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IImprovementSuggestionRepository : IRepository<ImprovementSuggestion>
{
    Task<List<ImprovementSuggestion>> GetByReviewRequestIdAsync(int reviewRequestId);
    Task<List<ImprovementSuggestion>> GetByIdsAsync(List<int> ids);
    Task DeleteByReviewRequestIdAsync(int reviewRequestId);
}