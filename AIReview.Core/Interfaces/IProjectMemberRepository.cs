using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IProjectMemberRepository : IRepository<ProjectMember>
{
    Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(int projectId);
    Task<ProjectMember?> GetMemberAsync(int projectId, string userId);
    Task<bool> IsMemberAsync(int projectId, string userId);
}