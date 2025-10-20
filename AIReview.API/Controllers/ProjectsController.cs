using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetProjects(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var projects = await _projectService.GetProjectsByUserAsync(userId, search, isActive);
            
            return Ok(new ApiResponse<IEnumerable<ProjectDto>>
            {
                Success = true,
                Data = projects
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects");
            return StatusCode(500, new ApiResponse<IEnumerable<ProjectDto>>
            {
                Success = false,
                Message = "获取项目列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = GetCurrentUserId();
            var project = await _projectService.CreateProjectAsync(request, userId);
            
            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new ApiResponse<ProjectDto>
            {
                Success = true,
                Data = project,
                Message = "项目创建成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "创建项目失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _projectService.HasProjectAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var project = await _projectService.GetProjectAsync(id);
            if (project == null)
            {
                return NotFound(new ApiResponse<ProjectDto>
                {
                    Success = false,
                    Message = "项目不存在"
                });
            }

            return Ok(new ApiResponse<ProjectDto>
            {
                Success = true,
                Data = project
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "获取项目详情失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, userId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            var project = await _projectService.UpdateProjectAsync(id, request);
            
            return Ok(new ApiResponse<ProjectDto>
            {
                Success = true,
                Data = project,
                Message = "项目更新成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "更新项目失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProject(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, userId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            await _projectService.DeleteProjectAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "项目删除成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除项目失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}/archive")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> ArchiveProject(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, userId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            var project = await _projectService.ArchiveProjectAsync(id);
            
            return Ok(new ApiResponse<ProjectDto>
            {
                Success = true,
                Data = project,
                Message = "项目已归档"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "归档项目失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}/unarchive")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UnarchiveProject(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, userId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            var project = await _projectService.UnarchiveProjectAsync(id);
            
            return Ok(new ApiResponse<ProjectDto>
            {
                Success = true,
                Data = project,
                Message = "项目已取消归档"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<ProjectDto>
            {
                Success = false,
                Message = "取消归档项目失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectMemberDto>>>> GetProjectMembers(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var hasAccess = await _projectService.HasProjectAccessAsync(id, userId);
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var members = await _projectService.GetProjectMembersAsync(id);
            
            return Ok(new ApiResponse<IEnumerable<ProjectMemberDto>>
            {
                Success = true,
                Data = members
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<IEnumerable<ProjectMemberDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project members for {ProjectId}", id);
            return StatusCode(500, new ApiResponse<IEnumerable<ProjectMemberDto>>
            {
                Success = false,
                Message = "获取项目成员失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<ApiResponse<object>>> AddProjectMember(int id, [FromBody] AddMemberRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, userId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            await _projectService.AddProjectMemberAsync(id, request);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "成员添加成功"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "添加成员失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveProjectMember(int id, string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, currentUserId);
            
            if (!isOwner)
            {
                return Forbid();
            }

            await _projectService.RemoveProjectMemberAsync(id, userId);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "成员移除成功"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from project {ProjectId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "移除成员失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPut("{id}/members/{userId}")]
    public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> UpdateProjectMemberRole(
        int id, string userId, [FromBody] UpdateProjectMemberRoleRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isOwner = await _projectService.IsProjectOwnerAsync(id, currentUserId);

            if (!isOwner)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProjectMemberDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var updated = await _projectService.UpdateProjectMemberRoleAsync(id, userId, request.Role);

            return Ok(new ApiResponse<ProjectMemberDto>
            {
                Success = true,
                Data = updated
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<ProjectMemberDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<ProjectMemberDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project member role for {ProjectId}", id);
            return StatusCode(500, new ApiResponse<ProjectMemberDto>
            {
                Success = false,
                Message = "更新成员角色失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("用户未认证");
    }
}