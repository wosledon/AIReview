using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class ReviewCommentRepository : Repository<ReviewComment>, IReviewCommentRepository
{
    public ReviewCommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ReviewComment>> GetCommentsByReviewAsync(int reviewId)
    {
        return await _dbSet
                .Include(c => c.Author)
            .Where(c => c.ReviewRequestId == reviewId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewComment>> GetCommentsByAuthorAsync(string authorId)
    {
        return await _dbSet
            .Where(c => c.AuthorId == authorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewComment>> GetAIGeneratedCommentsAsync(int reviewId)
    {
        return await _dbSet
            .Where(c => c.ReviewRequestId == reviewId && c.IsAIGenerated)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewComment>> GetCommentsByFileAsync(int reviewId, string filePath)
    {
        return await _dbSet
            .Where(c => c.ReviewRequestId == reviewId && c.FilePath == filePath)
            .OrderBy(c => c.LineNumber ?? 0)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public override async Task<ReviewComment?> GetByIdAsync(int id)
    {
           return await _dbSet
              .Include(c => c.Author)
              .FirstOrDefaultAsync(c => c.Id == id);
    }
}