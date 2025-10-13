using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;

namespace AIReview.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class GitController : ControllerBase
{
    private readonly IGitService _gitService;
    private readonly ILogger<GitController> _logger;

    public GitController(IGitService gitService, ILogger<GitController> logger)
    {
        _gitService = gitService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有仓库
    /// </summary>
    [HttpGet("repositories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GitRepositoryDto>>>> GetRepositories([FromQuery] int? projectId = null)
    {
        try
        {
            var repositories = await _gitService.GetRepositoriesAsync(projectId);
            var dtos = repositories.Select(MapToDto);
            
            return Ok(new ApiResponse<IEnumerable<GitRepositoryDto>>
            {
                Success = true,
                Data = dtos,
                Message = "仓库列表获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repositories");
            return StatusCode(500, new ApiResponse<IEnumerable<GitRepositoryDto>>
            {
                Success = false,
                Message = "获取仓库列表失败"
            });
        }
    }

    /// <summary>
    /// 根据ID获取仓库
    /// </summary>
    [HttpGet("repositories/{id}")]
    public async Task<ActionResult<ApiResponse<GitRepositoryDto>>> GetRepository(int id)
    {
        try
        {
            var repository = await _gitService.GetRepositoryAsync(id);
            if (repository == null)
            {
                return NotFound(new ApiResponse<GitRepositoryDto>
                {
                    Success = false,
                    Message = "仓库不存在"
                });
            }

            return Ok(new ApiResponse<GitRepositoryDto>
            {
                Success = true,
                Data = MapToDto(repository),
                Message = "仓库信息获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository {Id}", id);
            return StatusCode(500, new ApiResponse<GitRepositoryDto>
            {
                Success = false,
                Message = "获取仓库信息失败"
            });
        }
    }

    /// <summary>
    /// 创建新仓库
    /// </summary>
    [HttpPost("repositories")]
    public async Task<ActionResult<ApiResponse<GitRepositoryDto>>> CreateRepository([FromBody] CreateGitRepositoryDto dto)
    {
        try
        {
            var repository = new GitRepository
            {
                Name = dto.Name,
                Url = dto.Url,
                DefaultBranch = dto.DefaultBranch ?? "main",
                Username = dto.Username,
                AccessToken = dto.AccessToken,
                ProjectId = dto.ProjectId,
                IsActive = true
            };

            var created = await _gitService.CreateRepositoryAsync(repository);
            
            return Ok(new ApiResponse<GitRepositoryDto>
            {
                Success = true,
                Data = MapToDto(created),
                Message = "仓库创建成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repository");
            return StatusCode(500, new ApiResponse<GitRepositoryDto>
            {
                Success = false,
                Message = "创建仓库失败"
            });
        }
    }

    /// <summary>
    /// 更新仓库
    /// </summary>
    [HttpPut("repositories/{id}")]
    public async Task<ActionResult<ApiResponse<GitRepositoryDto>>> UpdateRepository(int id, [FromBody] UpdateGitRepositoryDto dto)
    {
        try
        {
            var repository = await _gitService.GetRepositoryAsync(id);
            if (repository == null)
            {
                return NotFound(new ApiResponse<GitRepositoryDto>
                {
                    Success = false,
                    Message = "仓库不存在"
                });
            }

            repository.Name = dto.Name;
            repository.Url = dto.Url;
            repository.DefaultBranch = dto.DefaultBranch;
            repository.Username = dto.Username;
            repository.AccessToken = dto.AccessToken;
            repository.IsActive = dto.IsActive;

            var updated = await _gitService.UpdateRepositoryAsync(repository);
            
            return Ok(new ApiResponse<GitRepositoryDto>
            {
                Success = true,
                Data = MapToDto(updated),
                Message = "仓库更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repository {Id}", id);
            return StatusCode(500, new ApiResponse<GitRepositoryDto>
            {
                Success = false,
                Message = "更新仓库失败"
            });
        }
    }

    /// <summary>
    /// 删除仓库
    /// </summary>
    [HttpDelete("repositories/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRepository(int id)
    {
        try
        {
            var success = await _gitService.DeleteRepositoryAsync(id);
            if (!success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "仓库不存在"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "仓库删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repository {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除仓库失败"
            });
        }
    }

    /// <summary>
    /// 克隆仓库
    /// </summary>
    [HttpPost("repositories/{id}/clone")]
    public async Task<ActionResult<ApiResponse<object>>> CloneRepository(int id)
    {
        try
        {
            var success = await _gitService.CloneRepositoryAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = success,
                Message = success ? "仓库克隆成功" : "仓库克隆失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning repository {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "克隆仓库失败"
            });
        }
    }

    /// <summary>
    /// 拉取仓库最新代码
    /// </summary>
    [HttpPost("repositories/{id}/pull")]
    public async Task<ActionResult<ApiResponse<object>>> PullRepository(int id, [FromQuery] string? branch = null)
    {
        try
        {
            var success = await _gitService.PullRepositoryAsync(id, branch);
            
            return Ok(new ApiResponse<object>
            {
                Success = success,
                Message = success ? "代码拉取成功" : "代码拉取失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling repository {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "拉取代码失败"
            });
        }
    }

    /// <summary>
    /// 同步仓库
    /// </summary>
    [HttpPost("repositories/{id}/sync")]
    public async Task<ActionResult<ApiResponse<object>>> SyncRepository(int id)
    {
        try
        {
            var success = await _gitService.SyncRepositoryAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = success,
                Message = success ? "仓库同步成功" : "仓库同步失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing repository {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "同步仓库失败"
            });
        }
    }

    /// <summary>
    /// 获取仓库分支
    /// </summary>
    [HttpGet("repositories/{id}/branches")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GitBranchDto>>>> GetBranches(int id)
    {
        try
        {
            var branches = await _gitService.GetBranchesAsync(id);
            var dtos = branches.Select(b => new GitBranchDto
            {
                Id = b.Id,
                Name = b.Name,
                CommitSha = b.CommitSha,
                IsDefault = b.IsDefault,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            });
            
            return Ok(new ApiResponse<IEnumerable<GitBranchDto>>
            {
                Success = true,
                Data = dtos,
                Message = "分支列表获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches for repository {Id}", id);
            return StatusCode(500, new ApiResponse<IEnumerable<GitBranchDto>>
            {
                Success = false,
                Message = "获取分支列表失败"
            });
        }
    }

    /// <summary>
    /// 获取仓库提交历史
    /// </summary>
    [HttpGet("repositories/{id}/commits")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GitCommitDto>>>> GetCommits(
        int id, 
        [FromQuery] string? branch = null, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        try
        {
            var commits = await _gitService.GetCommitsAsync(id, branch, skip, take);
            var dtos = commits.Select(c => new GitCommitDto
            {
                Id = c.Id,
                Sha = c.Sha,
                Message = c.Message,
                AuthorName = c.AuthorName,
                AuthorEmail = c.AuthorEmail,
                AuthorDate = c.AuthorDate,
                CommitterName = c.CommitterName,
                CommitterEmail = c.CommitterEmail,
                CommitterDate = c.CommitterDate,
                BranchName = c.BranchName,
                FileChangesCount = c.FileChanges.Count
            });
            
            return Ok(new ApiResponse<IEnumerable<GitCommitDto>>
            {
                Success = true,
                Data = dtos,
                Message = "提交历史获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commits for repository {Id}", id);
            return StatusCode(500, new ApiResponse<IEnumerable<GitCommitDto>>
            {
                Success = false,
                Message = "获取提交历史失败"
            });
        }
    }

    /// <summary>
    /// 获取提交详情
    /// </summary>
    [HttpGet("repositories/{id}/commits/{sha}")]
    public async Task<ActionResult<ApiResponse<GitCommitDetailDto>>> GetCommit(int id, string sha)
    {
        try
        {
            var commit = await _gitService.GetCommitAsync(id, sha);
            if (commit == null)
            {
                return NotFound(new ApiResponse<GitCommitDetailDto>
                {
                    Success = false,
                    Message = "提交不存在"
                });
            }

            var dto = new GitCommitDetailDto
            {
                Id = commit.Id,
                Sha = commit.Sha,
                Message = commit.Message,
                AuthorName = commit.AuthorName,
                AuthorEmail = commit.AuthorEmail,
                AuthorDate = commit.AuthorDate,
                CommitterName = commit.CommitterName,
                CommitterEmail = commit.CommitterEmail,
                CommitterDate = commit.CommitterDate,
                BranchName = commit.BranchName,
                FileChanges = commit.FileChanges.Select(f => new GitFileChangeDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    ChangeType = f.ChangeType,
                    AddedLines = f.AddedLines,
                    DeletedLines = f.DeletedLines
                }).ToList()
            };
            
            return Ok(new ApiResponse<GitCommitDetailDto>
            {
                Success = true,
                Data = dto,
                Message = "提交详情获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commit {Sha} for repository {Id}", sha, id);
            return StatusCode(500, new ApiResponse<GitCommitDetailDto>
            {
                Success = false,
                Message = "获取提交详情失败"
            });
        }
    }

    /// <summary>
    /// 测试仓库连接
    /// </summary>
    [HttpPost("repositories/{id}/test")]
    public async Task<ActionResult<ApiResponse<object>>> TestRepository(int id)
    {
        try
        {
            var accessible = await _gitService.IsRepositoryAccessibleAsync(id);
            
            return Ok(new ApiResponse<object>
            {
                Success = accessible,
                Message = accessible ? "仓库连接正常" : "仓库连接失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing repository {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "测试仓库连接失败"
            });
        }
    }

    /// <summary>
    /// 获取指定分支之间的代码差异
    /// </summary>
    [HttpGet("repositories/{id}/diff")]
    public async Task<ActionResult<ApiResponse<string>>> GetRepositoryDiff(
        int id, 
        [FromQuery] string targetBranch, 
        [FromQuery] string baseBranch = "main")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "目标分支不能为空"
                });
            }

            var diff = await _gitService.GetDiffBetweenRefsAsync(id, baseBranch, targetBranch);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = diff,
                Message = "差异获取成功"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff for repository {Id}, base: {BaseBranch}, target: {TargetBranch}", 
                id, baseBranch, targetBranch);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "获取代码差异失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private static GitRepositoryDto MapToDto(GitRepository repository)
    {
        return new GitRepositoryDto
        {
            Id = repository.Id,
            Name = repository.Name,
            Url = repository.Url,
            LocalPath = repository.LocalPath,
            DefaultBranch = repository.DefaultBranch,
            Username = repository.Username,
            IsActive = repository.IsActive,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
            LastSyncAt = repository.LastSyncAt,
            ProjectId = repository.ProjectId,
            ProjectName = repository.Project?.Name,
            BranchCount = repository.Branches.Count
        };
    }
}