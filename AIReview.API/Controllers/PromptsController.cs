using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PromptsController : ControllerBase
{
    private readonly IPromptService _promptService;
    private readonly IProjectService _projectService;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IPromptService promptService, IProjectService projectService, ILogger<PromptsController> logger)
    {
        _promptService = promptService;
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet("user")] 
    public async Task<ActionResult<ApiResponse<IEnumerable<PromptDto>>>> ListUserPrompts()
    {
        var userId = GetCurrentUserId();
        var list = await _promptService.ListUserPromptsAsync(userId);
        return Ok(new ApiResponse<IEnumerable<PromptDto>> { Success = true, Data = list });
    }

    [HttpGet("built-in")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PromptDto>>>> ListBuiltInPrompts()
    {
        var list = await _promptService.ListBuiltInPromptsAsync();
        return Ok(new ApiResponse<IEnumerable<PromptDto>> { Success = true, Data = list });
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PromptDto>>>> ListProjectPrompts(int projectId)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _projectService.HasProjectAccessAsync(projectId, userId);
        if (!hasAccess) return Forbid();
        var list = await _promptService.ListProjectPromptsAsync(projectId);
        return Ok(new ApiResponse<IEnumerable<PromptDto>> { Success = true, Data = list });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PromptDto>>> Create([FromBody] CreatePromptRequest req)
    {
        var userId = GetCurrentUserId();
        if (req.ProjectId.HasValue)
        {
            var hasAccess = await _projectService.IsProjectOwnerAsync(req.ProjectId.Value, userId);
            if (!hasAccess) return Forbid();
        }
        else
        {
            // 如果不是项目级模板,则自动设置为当前用户的模板
            req.UserId = userId;
        }

        if (!string.IsNullOrEmpty(req.UserId) && req.UserId != userId)
        {
            return Forbid();
        }

        var dto = await _promptService.CreateAsync(req, userId);
        return Ok(new ApiResponse<PromptDto> { Success = true, Data = dto, Message = "创建成功" });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PromptDto>>> Update(int id, [FromBody] UpdatePromptRequest req)
    {
        var userId = GetCurrentUserId();
        var dto = await _promptService.UpdateAsync(id, req, userId);
        return Ok(new ApiResponse<PromptDto> { Success = true, Data = dto, Message = "更新成功" });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _promptService.DeleteAsync(id, userId);
        return Ok(new ApiResponse<object> { Success = true, Message = "已删除" });
    }

    [HttpGet("effective")]
    public async Task<ActionResult<ApiResponse<EffectivePromptResponse>>> GetEffective([FromQuery] PromptType type, [FromQuery] int? projectId)
    {
        var userId = GetCurrentUserId();
        if (projectId.HasValue)
        {
            var hasAccess = await _projectService.HasProjectAccessAsync(projectId.Value, userId);
            if (!hasAccess) return Forbid();
        }
        var effective = await _promptService.GetEffectivePromptAsync(type, userId, projectId);
        return Ok(new ApiResponse<EffectivePromptResponse> { Success = true, Data = effective });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
