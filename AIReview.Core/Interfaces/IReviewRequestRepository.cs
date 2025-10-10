using AIReview.Core.Entities;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.Core.Interfaces;

public interface IReviewRequestRepository : IRepository<ReviewRequest>
{
    Task<PagedResult<ReviewRequest>> GetPagedReviewsAsync(ReviewQueryParameters parameters);
    Task<IEnumerable<ReviewRequest>> GetReviewsByProjectAsync(int projectId);
    Task<IEnumerable<ReviewRequest>> GetReviewsByAuthorAsync(string authorId);
    Task<IEnumerable<ReviewRequest>> GetReviewsByStatusAsync(ReviewState status);
    Task<ReviewRequest?> GetReviewWithCommentsAsync(int reviewId);
    Task<ReviewRequest?> GetReviewWithProjectAsync(int reviewId);
}