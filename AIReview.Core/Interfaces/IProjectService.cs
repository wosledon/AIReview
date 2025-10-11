using AIReview.Core.Entities;
using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, string ownerId);
    Task<ProjectDto?> GetProjectAsync(int id);
    Task<IEnumerable<ProjectDto>> GetProjectsByUserAsync(string userId);
    Task<IEnumerable<ProjectDto>> GetProjectsByUserAsync(string userId, string? search, bool? isActive);
    Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectRequest request);
    Task DeleteProjectAsync(int id);
    Task<ProjectDto> ArchiveProjectAsync(int id);
    Task<ProjectDto> UnarchiveProjectAsync(int id);
    
    Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(int projectId);
    Task AddProjectMemberAsync(int projectId, AddMemberRequest request);
    Task RemoveProjectMemberAsync(int projectId, string userId);
    Task<bool> HasProjectAccessAsync(int projectId, string userId);
    Task<bool> IsProjectOwnerAsync(int projectId, string userId);
}