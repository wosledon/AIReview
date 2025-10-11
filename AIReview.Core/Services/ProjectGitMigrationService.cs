using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIReview.Core.Services;

/// <summary>
/// 项目Git仓库关联管理服务
/// </summary>
public class ProjectGitMigrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGitService _gitService;
    private readonly ILogger<ProjectGitMigrationService> _logger;

    public ProjectGitMigrationService(
        IUnitOfWork unitOfWork,
        IGitService gitService,
        ILogger<ProjectGitMigrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _gitService = gitService;
        _logger = logger;
    }

    /// <summary>
    /// 为所有现有项目创建Git仓库记录
    /// </summary>
    public async Task MigrateExistingProjectsAsync()
    {
        try
        {
            _logger.LogInformation("Starting migration to create Git repositories for existing projects");

            // 获取所有有仓库URL但没有关联Git仓库的项目
            var projectsWithoutGitRepo = await GetProjectsWithoutGitRepositoryAsync();
            
            var migrationCount = 0;
            var errors = new List<string>();

            foreach (var project in projectsWithoutGitRepo)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(project.RepositoryUrl))
                    {
                        await CreateGitRepositoryForProjectAsync(project);
                        migrationCount++;
                        _logger.LogInformation("Created Git repository for project {ProjectId}: {ProjectName}", 
                            project.Id, project.Name);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to create Git repository for project {project.Id} ({project.Name}): {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, error);
                }
            }

            _logger.LogInformation("Migration completed. Created {Count} Git repositories. Errors: {ErrorCount}", 
                migrationCount, errors.Count);

            if (errors.Any())
            {
                _logger.LogWarning("Migration errors:\n{Errors}", string.Join("\n", errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete project Git repository migration");
            throw;
        }
    }

    /// <summary>
    /// 获取有仓库URL但没有Git仓库记录的项目
    /// </summary>
    private async Task<IEnumerable<Project>> GetProjectsWithoutGitRepositoryAsync()
    {
        var allProjects = await _unitOfWork.Projects.GetAllAsync();
        var projectsWithoutGitRepo = new List<Project>();

        foreach (var project in allProjects)
        {
            if (!string.IsNullOrWhiteSpace(project.RepositoryUrl))
            {
                var existingRepos = await _gitService.GetRepositoriesAsync(project.Id);
                if (!existingRepos.Any())
                {
                    projectsWithoutGitRepo.Add(project);
                }
            }
        }

        return projectsWithoutGitRepo;
    }

    /// <summary>
    /// 为单个项目创建Git仓库记录
    /// </summary>
    private async Task CreateGitRepositoryForProjectAsync(Project project)
    {
        var repoName = ExtractRepositoryNameFromUrl(project.RepositoryUrl!);
        
        var gitRepository = new GitRepository
        {
            Name = repoName,
            Url = project.RepositoryUrl!,
            ProjectId = project.Id,
            DefaultBranch = "main",
            IsActive = true
        };

        await _gitService.CreateRepositoryAsync(gitRepository);
    }

    /// <summary>
    /// 从URL中提取仓库名称
    /// </summary>
    private string ExtractRepositoryNameFromUrl(string url)
    {
        try
        {
            // 处理常见的Git URL格式
            var normalizedUrl = url;
            
            // 处理SSH格式: git@github.com:user/repo.git
            if (url.StartsWith("git@"))
            {
                normalizedUrl = url.Replace("git@", "https://").Replace(":", "/");
            }
            
            var uri = new Uri(normalizedUrl);
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
            // 如果URL解析失败，使用时间戳生成唯一名称
            return $"repo-{DateTime.UtcNow.Ticks}";
        }
    }

    /// <summary>
    /// 检查指定项目是否已有Git仓库关联
    /// </summary>
    public async Task<bool> HasGitRepositoryAsync(int projectId)
    {
        var repos = await _gitService.GetRepositoriesAsync(projectId);
        return repos.Any();
    }

    /// <summary>
    /// 为单个项目手动创建Git仓库关联
    /// </summary>
    public async Task<bool> EnsureProjectHasGitRepositoryAsync(int projectId)
    {
        try
        {
            if (await HasGitRepositoryAsync(projectId))
            {
                return true; // 已经有Git仓库
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null || string.IsNullOrWhiteSpace(project.RepositoryUrl))
            {
                return false; // 项目不存在或没有仓库URL
            }

            await CreateGitRepositoryForProjectAsync(project);
            _logger.LogInformation("Created Git repository for project {ProjectId} on demand", projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Git repository for project {ProjectId}", projectId);
            return false;
        }
    }
}