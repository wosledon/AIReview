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
    private readonly IGitService _gitService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IGitService gitService,
        ILogger<ProjectService> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _gitService = gitService;
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

        // 如果提供了仓库URL，自动创建Git仓库记录
        if (!string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            try
            {
                await CreateGitRepositoryForProjectAsync(project, request.RepositoryUrl);
                _logger.LogInformation("Git repository created for project {ProjectId}", project.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Git repository for project {ProjectId}: {Error}", 
                    project.Id, ex.Message);
                // 不中断项目创建流程，Git仓库创建失败不应影响项目创建
            }
        }

        return await MapToProjectDtoAsync(project);
    }

    private async Task CreateGitRepositoryForProjectAsync(Project project, string repositoryUrl)
    {
        // 从URL中提取仓库名称
        var repoName = ExtractRepositoryNameFromUrl(repositoryUrl);
        
        var gitRepository = new GitRepository
        {
            Name = repoName,
            Url = repositoryUrl,
            ProjectId = project.Id,
            DefaultBranch = "main", // 默认分支
            IsActive = true
        };

        await _gitService.CreateRepositoryAsync(gitRepository);
    }

    private string ExtractRepositoryNameFromUrl(string url)
    {
        try
        {
            // 处理常见的Git URL格式
            // https://github.com/user/repo.git -> repo
            // https://github.com/user/repo -> repo
            // git@github.com:user/repo.git -> repo
            
            var uri = new Uri(url.Replace("git@", "https://").Replace(":", "/"));
            var segments = uri.Segments;
            var lastSegment = segments[segments.Length - 1];
            
            // 移除.git后缀
            if (lastSegment.EndsWith(".git"))
            {
                lastSegment = lastSegment.Substring(0, lastSegment.Length - 4);
            }
            
            // 移除尾部的斜杠
            return lastSegment.TrimEnd('/');
        }
        catch
        {
            // 如果URL解析失败，使用项目名称作为仓库名称
            return $"repo-{DateTime.UtcNow.Ticks}";
        }
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

    public async Task<IEnumerable<ProjectDto>> GetProjectsByUserAsync(string userId, string? search, bool? isActive)
    {
        var projects = await _unitOfWork.Projects.GetProjectsByMemberAsync(userId);
        
        // 应用搜索过滤
        if (!string.IsNullOrEmpty(search))
        {
            projects = projects.Where(p => 
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.RepositoryUrl) && p.RepositoryUrl.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
        }

        // 应用状态过滤
        if (isActive.HasValue)
        {
            projects = projects.Where(p => p.IsActive == isActive.Value);
        }

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

        var originalRepositoryUrl = project.RepositoryUrl;

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

        // 处理Git仓库URL变更
        if (!string.IsNullOrEmpty(request.RepositoryUrl) && 
            request.RepositoryUrl != originalRepositoryUrl)
        {
            try
            {
                await UpdateGitRepositoryForProjectAsync(project, request.RepositoryUrl);
                _logger.LogInformation("Git repository updated for project {ProjectId}", project.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update Git repository for project {ProjectId}: {Error}", 
                    project.Id, ex.Message);
            }
        }

        _logger.LogInformation("Project updated: {ProjectId}", project.Id);

        return await MapToProjectDtoAsync(project);
    }

    private async Task UpdateGitRepositoryForProjectAsync(Project project, string repositoryUrl)
    {
        // 查找项目关联的Git仓库
        var repositories = await _gitService.GetRepositoriesAsync(project.Id);
        var existingRepo = repositories.FirstOrDefault();

        if (existingRepo != null)
        {
            // 更新现有仓库
            existingRepo.Url = repositoryUrl;
            existingRepo.Name = ExtractRepositoryNameFromUrl(repositoryUrl);
            await _gitService.UpdateRepositoryAsync(existingRepo);
        }
        else
        {
            // 创建新的Git仓库记录
            await CreateGitRepositoryForProjectAsync(project, repositoryUrl);
        }
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

    public async Task<ProjectDto> ArchiveProjectAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            throw new ArgumentException("项目不存在");

        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return await MapToProjectDtoAsync(project);
    }

    public async Task<ProjectDto> UnarchiveProjectAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            throw new ArgumentException("项目不存在");

        project.IsActive = true;
        project.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();

        return await MapToProjectDtoAsync(project);
    }

    private async Task<ProjectDto> MapToProjectDtoAsync(Project project)
    {
        // 获取项目成员数量
        var memberCount = await _unitOfWork.ProjectMembers.CountMembersByProjectIdAsync(project.Id);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            RepositoryUrl = project.RepositoryUrl,
            Language = project.Language,
            IsActive = project.IsActive,
            MemberCount = memberCount,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}