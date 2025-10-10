using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetProjectsByOwnerAsync(string ownerId);
    Task<IEnumerable<Project>> GetProjectsByMemberAsync(string userId);
    Task<Project?> GetProjectWithMembersAsync(int projectId);
    Task<bool> IsUserProjectMemberAsync(int projectId, string userId);
    Task<bool> IsUserProjectOwnerAsync(int projectId, string userId);
}