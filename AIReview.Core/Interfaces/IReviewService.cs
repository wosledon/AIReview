using AIReview.Core.Entities;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.Core.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(CreateReviewRequest request, string authorId);
    Task<ReviewDto?> GetReviewAsync(int id);
    Task<PagedResult<ReviewDto>> GetReviewsAsync(ReviewQueryParameters parameters);
    Task<ReviewDto> UpdateReviewAsync(int id, UpdateReviewRequest request);
    Task DeleteReviewAsync(int id);
    Task UpdateReviewStatusAsync(int id, ReviewState status);
    
    Task<IEnumerable<ReviewCommentDto>> GetReviewCommentsAsync(int reviewId);
    Task<ReviewCommentDto> AddReviewCommentAsync(int reviewId, AddCommentRequest request, string authorId);
    Task<ReviewCommentDto?> GetReviewCommentAsync(int commentId);
    Task<ReviewCommentDto> UpdateReviewCommentAsync(int commentId, UpdateCommentRequest request);
    Task DeleteReviewCommentAsync(int commentId);
    
    Task<AIReviewResultDto?> GetAIReviewResultAsync(int reviewId);
    Task SaveAIReviewResultAsync(int reviewId, AIReviewResult result);
    Task<bool> HasReviewAccessAsync(int reviewId, string userId);
}