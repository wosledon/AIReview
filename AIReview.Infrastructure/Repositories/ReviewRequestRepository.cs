using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.Infrastructure.Repositories;

public class ReviewRequestRepository : Repository<ReviewRequest>, IReviewRequestRepository
{
    public ReviewRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<ReviewRequest>> GetPagedReviewsAsync(ReviewQueryParameters parameters)
    {
        var query = _dbSet
            .Include(r => r.Project)
            .Include(r => r.Author)
            .AsQueryable();

        // 应用过滤条件
        if (parameters.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == parameters.ProjectId.Value);
        }

        if (!string.IsNullOrEmpty(parameters.Status))
        {
            if (Enum.TryParse<ReviewState>(parameters.Status, out var status))
            {
                query = query.Where(r => r.Status == status);
            }
        }

        if (!string.IsNullOrEmpty(parameters.AuthorId))
        {
            query = query.Where(r => r.AuthorId == parameters.AuthorId);
        }

        if (parameters.CreatedAfter.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= parameters.CreatedAfter.Value);
        }

        if (parameters.CreatedBefore.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= parameters.CreatedBefore.Value);
        }

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var keyword = parameters.Search.Trim();
            query = query.Where(r =>
                EF.Functions.Like(r.Title, $"%{keyword}%") ||
                (r.Description != null && EF.Functions.Like(r.Description, $"%{keyword}%")) ||
                (r.Project != null && EF.Functions.Like(r.Project.Name, $"%{keyword}%"))
            );
        }

        // 计算总数
        var totalCount = await query.CountAsync();

        // 应用分页和排序
        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return new PagedResult<ReviewRequest>
        {
            Items = items,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / parameters.PageSize)
        };
    }

    public async Task<IEnumerable<ReviewRequest>> GetReviewsByProjectAsync(int projectId)
    {
        return await _dbSet
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewRequest>> GetReviewsByAuthorAsync(string authorId)
    {
        return await _dbSet
            .Where(r => r.AuthorId == authorId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReviewRequest>> GetReviewsByStatusAsync(ReviewState status)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<ReviewRequest?> GetReviewWithCommentsAsync(int reviewId)
    {
        return await _dbSet
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
    }

    public async Task<ReviewRequest?> GetReviewWithProjectAsync(int reviewId)
    {
        return await _dbSet
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
    }

    public override async Task<ReviewRequest?> GetByIdAsync(int id)
    {
        // 使用 FindAsync 提升与 InMemory Provider 的一致性
        return await _dbSet.FindAsync(id);
    }
}