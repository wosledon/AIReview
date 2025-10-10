using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

public interface IGitService
{
    // 仓库管理
    Task<GitRepository> CreateRepositoryAsync(GitRepository repository);
    Task<GitRepository?> GetRepositoryAsync(int id);
    Task<GitRepository?> GetRepositoryByUrlAsync(string url);
    Task<IEnumerable<GitRepository>> GetRepositoriesAsync(int? projectId = null);
    Task<GitRepository> UpdateRepositoryAsync(GitRepository repository);
    Task<bool> DeleteRepositoryAsync(int id);
    
    // Git操作
    Task<bool> CloneRepositoryAsync(int repositoryId);
    Task<bool> PullRepositoryAsync(int repositoryId, string? branch = null);
    Task<bool> SyncRepositoryAsync(int repositoryId);
    
    // 分支管理
    Task<IEnumerable<GitBranch>> GetBranchesAsync(int repositoryId);
    Task<GitBranch?> GetBranchAsync(int repositoryId, string branchName);
    Task<bool> SyncBranchesAsync(int repositoryId);
    
    // 提交管理
    Task<IEnumerable<GitCommit>> GetCommitsAsync(int repositoryId, string? branch = null, int skip = 0, int take = 50);
    Task<GitCommit?> GetCommitAsync(int repositoryId, string sha);
    Task<bool> SyncCommitsAsync(int repositoryId, string? branch = null, int? maxCommits = null);
    
    // 文件变更
    Task<IEnumerable<GitFileChange>> GetFileChangesAsync(int commitId);
    Task<string?> GetCommitDiffAsync(int repositoryId, string sha);
    
    // 仓库状态
    Task<bool> IsRepositoryAccessibleAsync(int repositoryId);
    Task<string?> GetLatestCommitShaAsync(int repositoryId, string? branch = null);
}