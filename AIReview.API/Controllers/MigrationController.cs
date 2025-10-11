using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIReview.Core.Services;
using AIReview.Shared.DTOs;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MigrationController : ControllerBase
{
    private readonly ProjectGitMigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(
        ProjectGitMigrationService migrationService,
        ILogger<MigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// 为所有现有项目创建Git仓库记录
    /// </summary>
    [HttpPost("migrate-git-repositories")]
    public async Task<ActionResult<ApiResponse<object>>> MigrateGitRepositories()
    {
        try
        {
            await _migrationService.MigrateExistingProjectsAsync();
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Git仓库迁移完成",
                Data = new { }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git repository migration failed");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Git仓库迁移失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 为指定项目创建Git仓库记录
    /// </summary>
    [HttpPost("projects/{projectId}/ensure-git-repository")]
    public async Task<ActionResult<ApiResponse<object>>> EnsureProjectGitRepository(int projectId)
    {
        try
        {
            var success = await _migrationService.EnsureProjectHasGitRepositoryAsync(projectId);
            
            if (success)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "项目Git仓库关联成功",
                    Data = new { ProjectId = projectId }
                });
            }
            else
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "无法为项目创建Git仓库关联，请检查项目是否存在且包含有效的仓库URL"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Git repository for project {ProjectId}", projectId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "创建项目Git仓库关联失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 检查项目是否已有Git仓库关联
    /// </summary>
    [HttpGet("projects/{projectId}/git-repository-status")]
    public async Task<ActionResult<ApiResponse<object>>> GetProjectGitRepositoryStatus(int projectId)
    {
        try
        {
            var hasGitRepo = await _migrationService.HasGitRepositoryAsync(projectId);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new 
                { 
                    ProjectId = projectId,
                    HasGitRepository = hasGitRepo
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Git repository status for project {ProjectId}", projectId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "检查项目Git仓库状态失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}