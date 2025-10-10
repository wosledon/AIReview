using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IReviewCommentRepository : IRepository<ReviewComment>
{
    Task<IEnumerable<ReviewComment>> GetCommentsByReviewAsync(int reviewId);
    Task<IEnumerable<ReviewComment>> GetCommentsByAuthorAsync(string authorId);
    Task<IEnumerable<ReviewComment>> GetAIGeneratedCommentsAsync(int reviewId);
    Task<IEnumerable<ReviewComment>> GetCommentsByFileAsync(int reviewId, string filePath);
}