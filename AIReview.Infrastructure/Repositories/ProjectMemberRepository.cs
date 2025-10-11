using Microsoft.EntityFrameworkCore;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Repositories;

public class ProjectMemberRepository : Repository<ProjectMember>, IProjectMemberRepository
{
    public ProjectMemberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(int projectId)
    {
        return await _dbSet
            .Where(pm => pm.ProjectId == projectId)
            .Include(pm => pm.User)
            .Include(pm => pm.Project)
            .OrderBy(pm => pm.JoinedAt)
            .ToListAsync();
    }

    public async Task<ProjectMember?> GetMemberAsync(int projectId, string userId)
    {
        return await _dbSet
            .Include(pm => pm.User)
            .Include(pm => pm.Project)
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<bool> IsMemberAsync(int projectId, string userId)
    {
        return await _dbSet
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<int> CountMembersByProjectIdAsync(int projectId)
    {
        return await _dbSet
            .Where(pm => pm.ProjectId == projectId)
            .CountAsync();
    }
}