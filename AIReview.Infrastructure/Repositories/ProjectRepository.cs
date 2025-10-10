using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Project>> GetProjectsByOwnerAsync(string ownerId)
    {
        return await _dbSet
            .Where(p => p.OwnerId == ownerId)
            .Include(p => p.Owner)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetProjectsByMemberAsync(string userId)
    {
        return await _dbSet
            .Where(p => p.Members.Any(m => m.UserId == userId) || p.OwnerId == userId)
            .Include(p => p.Owner)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectWithMembersAsync(int projectId)
    {
        return await _dbSet
            .Include(p => p.Owner)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<bool> IsUserProjectMemberAsync(int projectId, string userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<bool> IsUserProjectOwnerAsync(int projectId, string userId)
    {
        return await _dbSet
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId);
    }

    public override async Task<Project?> GetByIdAsync(int id)
    {
        // 使用 FindAsync 更好地利用跟踪缓存，在 InMemory 与关系型提供程序之间行为更一致
        return await _dbSet.FindAsync(id);
    }
}