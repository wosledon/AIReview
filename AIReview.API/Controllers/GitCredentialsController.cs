using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GitCredentialsController : ControllerBase
{
    private readonly IGitCredentialService _credentialService;
    private readonly ILogger<GitCredentialsController> _logger;

    public GitCredentialsController(IGitCredentialService credentialService, ILogger<GitCredentialsController> logger)
    {
        _credentialService = credentialService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<GitCredentialDto>>>> GetUserCredentials()
    {
        var userId = User?.Identity?.Name ?? "";
        var list = await _credentialService.GetUserCredentialsAsync(userId);
        return Ok(new ApiResponse<IEnumerable<GitCredentialDto>> { Success = true, Data = list });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GitCredentialDto>>> GetCredential(int id)
    {
        var userId = User?.Identity?.Name ?? "";
        var dto = await _credentialService.GetCredentialAsync(id, userId);
        if (dto == null) return NotFound(new ApiResponse<GitCredentialDto> { Success = false, Message = "凭证不存在" });
        return Ok(new ApiResponse<GitCredentialDto> { Success = true, Data = dto });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GitCredentialDto>>> CreateCredential([FromBody] CreateGitCredentialRequest request)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "";
            var dto = await _credentialService.CreateCredentialAsync(userId, request);
            return Ok(new ApiResponse<GitCredentialDto> { Success = true, Data = dto });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<GitCredentialDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credential");
            return StatusCode(500, new ApiResponse<GitCredentialDto> { Success = false, Message = "创建凭证失败" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<GitCredentialDto>>> UpdateCredential(int id, [FromBody] UpdateGitCredentialRequest request)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "";
            var dto = await _credentialService.UpdateCredentialAsync(id, userId, request);
            return Ok(new ApiResponse<GitCredentialDto> { Success = true, Data = dto });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<GitCredentialDto> { Success = false, Message = "凭证不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credential {Id}", id);
            return StatusCode(500, new ApiResponse<GitCredentialDto> { Success = false, Message = "更新凭证失败" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCredential(int id)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "";
            await _credentialService.DeleteCredentialAsync(id, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "删除成功" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "凭证不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential {Id}", id);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "删除凭证失败" });
        }
    }

    [HttpPost("generate-ssh-key")]
    public async Task<ActionResult<ApiResponse<SshKeyPairDto>>> GenerateSshKey()
    {
        var pair = await _credentialService.GenerateSshKeyPairAsync();
        return Ok(new ApiResponse<SshKeyPairDto> { Success = true, Data = pair });
    }

    [HttpPost("{id}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyCredential(int id, [FromQuery] string repositoryUrl)
    {
        var userId = User?.Identity?.Name ?? "";
        var ok = await _credentialService.VerifyCredentialAsync(id, userId, repositoryUrl);
        return Ok(new ApiResponse<object> { Success = ok, Message = ok ? "验证通过" : "验证失败" });
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult<ApiResponse<object>>> SetDefault(int id)
    {
        var userId = User?.Identity?.Name ?? "";
        await _credentialService.SetDefaultCredentialAsync(id, userId);
        return Ok(new ApiResponse<object> { Success = true, Message = "设置默认成功" });
    }
}
