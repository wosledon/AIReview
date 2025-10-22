using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// 仓库拉取/同步状态服务
/// </summary>
public interface IGitRepositoryStatusService
{
    Task<GitRepositoryStatusDto> GetAsync(int repositoryId);
    Task UpdateAsync(int repositoryId, Action<GitRepositoryStatusDto> updater);
    Task SetPullingAsync(int repositoryId, int? credentialId = null);
    Task SetSuccessAsync(int repositoryId, string? branch = null, string? lastCommitSha = null, int totalFiles = 0, long totalLines = 0);
    Task SetFailedAsync(int repositoryId, string errorMessage);
    Task SetProgressAsync(int repositoryId, int progress);
}