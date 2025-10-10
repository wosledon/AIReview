using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<ProjectService> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, string ownerId)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            RepositoryUrl = request.RepositoryUrl,
            Language = request.Language,
            OwnerId = ownerId
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Project created: {ProjectId} by {OwnerId}", project.Id, ownerId);

        return await MapToProjectDtoAsync(project);
    }

    public async Task<ProjectDto?> GetProjectAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            return null;

        return await MapToProjectDtoAsync(project);
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsByUserAsync(string userId)
    {
        var projects = await _unitOfWork.Projects.GetProjectsByMemberAsync(userId);
        var projectDtos = new List<ProjectDto>();

        foreach (var project in projects)
        {
            projectDtos.Add(await MapToProjectDtoAsync(project));
        }

        return projectDtos;
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectRequest request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            throw new ArgumentException($"Project with id {id} not found");

        if (!string.IsNullOrEmpty(request.Name))
            project.Name = request.Name;

        if (request.Description != null)
            project.Description = request.Description;

        if (!string.IsNullOrEmpty(request.RepositoryUrl))
            project.RepositoryUrl = request.RepositoryUrl;

        if (!string.IsNullOrEmpty(request.Language))
            project.Language = request.Language;

        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Project updated: {ProjectId}", project.Id);

        return await MapToProjectDtoAsync(project);
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            throw new ArgumentException($"Project with id {id} not found");

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Project deleted: {ProjectId}", id);
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetProjectMembersAsync(int projectId)
    {
        var project = await _unitOfWork.Projects.GetProjectWithMembersAsync(projectId);
        if (project == null)
            throw new ArgumentException($"Project with id {projectId} not found");

        var memberDtos = new List<ProjectMemberDto>();

        // 添加项目所有者
        memberDtos.Add(new ProjectMemberDto
        {
            Id = 0, // 所有者没有ProjectMember记录
            ProjectId = projectId,
            UserId = project.OwnerId,
            UserName = project.Owner.UserName ?? "",
            UserEmail = project.Owner.Email ?? "",
            Role = ProjectMemberRole.Owner.ToString(),
            JoinedAt = project.CreatedAt
        });

        // 添加其他成员
        foreach (var member in project.Members)
        {
            memberDtos.Add(new ProjectMemberDto
            {
                Id = member.Id,
                ProjectId = projectId,
                UserId = member.UserId,
                UserName = member.User.UserName ?? "",
                UserEmail = member.User.Email ?? "",
                Role = member.Role.ToString(),
                JoinedAt = member.JoinedAt
            });
        }

        return memberDtos;
    }

    public async Task AddProjectMemberAsync(int projectId, AddMemberRequest request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
            throw new ArgumentException($"Project with id {projectId} not found");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new ArgumentException($"User with email {request.Email} not found");

        // 检查是否已经是成员
        var existingMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(projectId, user.Id);
        if (existingMember)
            throw new InvalidOperationException("User is already a member of this project");

        if (!Enum.TryParse<ProjectMemberRole>(request.Role, out var role))
            role = ProjectMemberRole.Developer;

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = user.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProjectMembers.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Member added to project: {UserId} to {ProjectId}", user.Id, projectId);
    }

    public async Task RemoveProjectMemberAsync(int projectId, string userId)
    {
        var member = await _unitOfWork.ProjectMembers.SingleOrDefaultAsync(
            m => m.ProjectId == projectId && m.UserId == userId);

        if (member == null)
            throw new ArgumentException("User is not a member of this project");

        _unitOfWork.ProjectMembers.Remove(member);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Member removed from project: {UserId} from {ProjectId}", userId, projectId);
    }

    public async Task<bool> HasProjectAccessAsync(int projectId, string userId)
    {
        var isOwner = await _unitOfWork.Projects.IsUserProjectOwnerAsync(projectId, userId);
        if (isOwner) return true;

        return await _unitOfWork.Projects.IsUserProjectMemberAsync(projectId, userId);
    }

    public async Task<bool> IsProjectOwnerAsync(int projectId, string userId)
    {
        return await _unitOfWork.Projects.IsUserProjectOwnerAsync(projectId, userId);
    }

    private async Task<ProjectDto> MapToProjectDtoAsync(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            RepositoryUrl = project.RepositoryUrl,
            Language = project.Language,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}