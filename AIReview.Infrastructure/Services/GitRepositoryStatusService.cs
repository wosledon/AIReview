using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Services;

public class GitRepositoryStatusService : IGitRepositoryStatusService
{
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _db;

    public GitRepositoryStatusService(ApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<GitRepositoryStatusDto> GetAsync(int repositoryId)
    {
        var status = await _db.Set<GitRepositoryStatus>()
            .Include(s => s.Repository)
            .FirstOrDefaultAsync(s => s.RepositoryId == repositoryId);
        if (status == null)
        {
            status = new GitRepositoryStatus
            {
                RepositoryId = repositoryId,
                Status = GitPullStatus.Never,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Set<GitRepositoryStatus>().Add(status);
            await _uow.SaveChangesAsync();
        }
        return Map(status);
    }

    public async Task UpdateAsync(int repositoryId, Action<GitRepositoryStatusDto> updater)
    {
        var status = await _db.Set<GitRepositoryStatus>()
            .FirstOrDefaultAsync(s => s.RepositoryId == repositoryId);
        if (status == null)
        {
            status = new GitRepositoryStatus
            {
                RepositoryId = repositoryId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<GitRepositoryStatus>().Add(status);
        }
        var dto = Map(status);
        updater(dto);
        Apply(status, dto);
        status.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
    }

    public async Task SetPullingAsync(int repositoryId, int? credentialId = null)
    {
        await UpdateAsync(repositoryId, dto =>
        {
            dto.Status = nameof(GitPullStatus.Pulling);
            dto.IsPulling = true;
            dto.Progress = 0;
            dto.ErrorMessage = null;
            dto.GitCredentialId = credentialId;
            dto.LastPullTime = DateTime.UtcNow;
        });
    }

    public async Task SetSuccessAsync(int repositoryId, string? branch = null, string? lastCommitSha = null, int totalFiles = 0, long totalLines = 0)
    {
        await UpdateAsync(repositoryId, dto =>
        {
            dto.Status = nameof(GitPullStatus.Success);
            dto.IsPulling = false;
            dto.Progress = 100;
            dto.ErrorMessage = null;
            dto.CurrentBranch = branch ?? dto.CurrentBranch;
            dto.LastCommitHash = lastCommitSha ?? dto.LastCommitHash;
            dto.TotalFiles = totalFiles;
            dto.TotalLines = totalLines;
            dto.LastPullTime = DateTime.UtcNow;
        });
    }

    public async Task SetFailedAsync(int repositoryId, string errorMessage)
    {
        await UpdateAsync(repositoryId, dto =>
        {
            dto.Status = nameof(GitPullStatus.Failed);
            dto.IsPulling = false;
            dto.Progress = 0;
            dto.ErrorMessage = errorMessage;
            dto.LastPullTime = DateTime.UtcNow;
        });
    }

    public async Task SetProgressAsync(int repositoryId, int progress)
    {
        await UpdateAsync(repositoryId, dto =>
        {
            dto.IsPulling = true;
            dto.Progress = Math.Clamp(progress, 0, 100);
        });
    }

    private static GitRepositoryStatusDto Map(GitRepositoryStatus s)
    {
        return new GitRepositoryStatusDto
        {
            Id = s.Id,
            ProjectId = s.Repository.ProjectId ?? 0,
            CurrentBranch = s.CurrentBranch,
            LastCommitHash = s.LastCommitHash,
            LastPullTime = s.LastPullTime,
            Status = s.Status.ToString(),
            ErrorMessage = s.ErrorMessage,
            LocalPath = s.LocalPath,
            TotalFiles = s.TotalFiles,
            TotalLines = s.TotalLines,
            GitCredentialId = s.GitCredentialId,
            IsPulling = s.IsPulling,
            Progress = s.Progress,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    private static void Apply(GitRepositoryStatus entity, GitRepositoryStatusDto dto)
    {
        entity.CurrentBranch = dto.CurrentBranch;
        entity.LastCommitHash = dto.LastCommitHash;
        entity.LastPullTime = dto.LastPullTime;
        entity.Status = Enum.TryParse<GitPullStatus>(dto.Status, out var ps) ? ps : entity.Status;
        entity.ErrorMessage = dto.ErrorMessage;
        entity.LocalPath = dto.LocalPath;
        entity.TotalFiles = dto.TotalFiles;
        entity.TotalLines = dto.TotalLines;
        entity.GitCredentialId = dto.GitCredentialId;
        entity.IsPulling = dto.IsPulling;
        entity.Progress = dto.Progress;
        if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;
    }
}
