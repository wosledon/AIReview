using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AIReview.Infrastructure.Services;

public class GitService : IGitService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GitService> _logger;

    public GitService(ApplicationDbContext context, ILogger<GitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 仓库管理

    public async Task<GitRepository> CreateRepositoryAsync(GitRepository repository)
    {
        _context.GitRepositories.Add(repository);
        await _context.SaveChangesAsync();
        return repository;
    }

    public async Task<GitRepository?> GetRepositoryAsync(int id)
    {
        return await _context.GitRepositories
            .Include(r => r.Project)
            .Include(r => r.Branches)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<GitRepository?> GetRepositoryByUrlAsync(string url)
    {
        return await _context.GitRepositories
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Url == url);
    }

    public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync(int? projectId = null)
    {
        var query = _context.GitRepositories
            .Include(r => r.Project)
            .Include(r => r.Branches)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == projectId.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<GitRepository> UpdateRepositoryAsync(GitRepository repository)
    {
        _context.GitRepositories.Update(repository);
        await _context.SaveChangesAsync();
        return repository;
    }

    public async Task<bool> DeleteRepositoryAsync(int id)
    {
        var repository = await _context.GitRepositories.FindAsync(id);
        if (repository == null) return false;

        // 删除本地仓库目录（如果存在）
        if (!string.IsNullOrEmpty(repository.LocalPath) && Directory.Exists(repository.LocalPath))
        {
            try
            {
                Directory.Delete(repository.LocalPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete local repository directory: {LocalPath}", repository.LocalPath);
            }
        }

        _context.GitRepositories.Remove(repository);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Git操作

    public async Task<bool> CloneRepositoryAsync(int repositoryId)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null) return false;

        try
        {
            // 生成本地路径
            var localPath = GenerateLocalPath(repository);
            repository.LocalPath = localPath;

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            // 构建git clone命令
            var cloneCommand = BuildGitCloneCommand(repository);
            
            var result = await ExecuteGitCommandAsync(cloneCommand, Path.GetDirectoryName(localPath)!);
            
            if (result.Success)
            {
                repository.LastSyncAt = DateTime.UtcNow;
                await UpdateRepositoryAsync(repository);
                
                // 同步分支和提交信息
                await SyncBranchesAsync(repositoryId);
                await SyncCommitsAsync(repositoryId, repository.DefaultBranch, 100);
                
                return true;
            }
            
            _logger.LogError("Git clone failed: {Error}", result.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning repository {RepositoryId}", repositoryId);
            return false;
        }
    }

    public async Task<bool> PullRepositoryAsync(int repositoryId, string? branch = null)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return false;
        }

        try
        {
            // 获取当前分支
            if (string.IsNullOrEmpty(branch))
            {
                branch = repository.DefaultBranch ?? "main";
            }

            // 切换到指定分支
            var checkoutResult = await ExecuteGitCommandAsync($"checkout {branch}", repository.LocalPath);
            if (!checkoutResult.Success)
            {
                _logger.LogWarning("Failed to checkout branch {Branch}: {Error}", branch, checkoutResult.Error);
            }

            // 执行pull
            var pullResult = await ExecuteGitCommandAsync("pull origin " + branch, repository.LocalPath);
            
            if (pullResult.Success)
            {
                repository.LastSyncAt = DateTime.UtcNow;
                await UpdateRepositoryAsync(repository);
                
                // 同步最新的提交信息
                await SyncCommitsAsync(repositoryId, branch, 50);
                
                return true;
            }

            _logger.LogError("Git pull failed: {Error}", pullResult.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling repository {RepositoryId}", repositoryId);
            return false;
        }
    }

    public async Task<bool> SyncRepositoryAsync(int repositoryId)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null) return false;

        // 如果本地路径不存在，尝试克隆
        if (string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return await CloneRepositoryAsync(repositoryId);
        }

        // 否则执行pull
        return await PullRepositoryAsync(repositoryId);
    }

    #endregion

    #region 分支管理

    public async Task<IEnumerable<GitBranch>> GetBranchesAsync(int repositoryId)
    {
        return await _context.GitBranches
            .Where(b => b.RepositoryId == repositoryId)
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<GitBranch?> GetBranchAsync(int repositoryId, string branchName)
    {
        return await _context.GitBranches
            .FirstOrDefaultAsync(b => b.RepositoryId == repositoryId && b.Name == branchName);
    }

    public async Task<bool> SyncBranchesAsync(int repositoryId)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return false;
        }

        try
        {
            // 获取远程分支列表
            var branchResult = await ExecuteGitCommandAsync("branch -r", repository.LocalPath);
            if (!branchResult.Success) return false;

            var remoteBranches = ParseRemoteBranches(branchResult.Output);
            
            foreach (var branchInfo in remoteBranches)
            {
                var existingBranch = await GetBranchAsync(repositoryId, branchInfo.Name);
                
                if (existingBranch == null)
                {
                    var newBranch = new GitBranch
                    {
                        RepositoryId = repositoryId,
                        Name = branchInfo.Name,
                        CommitSha = branchInfo.CommitSha,
                        IsDefault = branchInfo.Name == repository.DefaultBranch
                    };
                    
                    _context.GitBranches.Add(newBranch);
                }
                else if (existingBranch.CommitSha != branchInfo.CommitSha)
                {
                    existingBranch.CommitSha = branchInfo.CommitSha;
                    existingBranch.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing branches for repository {RepositoryId}", repositoryId);
            return false;
        }
    }

    #endregion

    #region 提交管理

    public async Task<IEnumerable<GitCommit>> GetCommitsAsync(int repositoryId, string? branch = null, int skip = 0, int take = 50)
    {
        var query = _context.GitCommits
            .Where(c => c.RepositoryId == repositoryId);

        if (!string.IsNullOrEmpty(branch))
        {
            query = query.Where(c => c.BranchName == branch);
        }

        return await query
            .OrderByDescending(c => c.AuthorDate)
            .Skip(skip)
            .Take(take)
            .Include(c => c.FileChanges)
            .ToListAsync();
    }

    public async Task<GitCommit?> GetCommitAsync(int repositoryId, string sha)
    {
        return await _context.GitCommits
            .Include(c => c.FileChanges)
            .FirstOrDefaultAsync(c => c.RepositoryId == repositoryId && c.Sha == sha);
    }

    public async Task<bool> SyncCommitsAsync(int repositoryId, string? branch = null, int? maxCommits = null)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return false;
        }

        try
        {
            if (string.IsNullOrEmpty(branch))
            {
                branch = repository.DefaultBranch ?? "main";
            }

            var limit = maxCommits?.ToString() ?? "100";
            var logCommand = $"log --oneline --format=\"%H|%an|%ae|%ad|%cn|%ce|%cd|%s\" --date=iso -{limit} {branch}";
            
            var logResult = await ExecuteGitCommandAsync(logCommand, repository.LocalPath);
            if (!logResult.Success) return false;

            var commits = ParseCommitLog(logResult.Output);
            
            foreach (var commitInfo in commits)
            {
                var existingCommit = await GetCommitAsync(repositoryId, commitInfo.Sha);
                
                if (existingCommit == null)
                {
                    var newCommit = new GitCommit
                    {
                        RepositoryId = repositoryId,
                        Sha = commitInfo.Sha,
                        Message = commitInfo.Message,
                        AuthorName = commitInfo.AuthorName,
                        AuthorEmail = commitInfo.AuthorEmail,
                        AuthorDate = commitInfo.AuthorDate,
                        CommitterName = commitInfo.CommitterName,
                        CommitterEmail = commitInfo.CommitterEmail,
                        CommitterDate = commitInfo.CommitterDate,
                        BranchName = branch
                    };
                    
                    _context.GitCommits.Add(newCommit);
                    await _context.SaveChangesAsync();
                    
                    // 同步文件变更
                    await SyncFileChangesAsync(newCommit.Id, repository.LocalPath, commitInfo.Sha);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing commits for repository {RepositoryId}", repositoryId);
            return false;
        }
    }

    #endregion

    #region 文件变更

    public async Task<IEnumerable<GitFileChange>> GetFileChangesAsync(int commitId)
    {
        return await _context.GitFileChanges
            .Where(f => f.CommitId == commitId)
            .OrderBy(f => f.FileName)
            .ToListAsync();
    }

    public async Task<string?> GetCommitDiffAsync(int repositoryId, string sha)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return null;
        }

        var diffResult = await ExecuteGitCommandAsync($"show {sha}", repository.LocalPath);
        return diffResult.Success ? diffResult.Output : null;
    }

    public async Task<string?> GetDiffBetweenRefsAsync(int repositoryId, string @base, string head)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return null;
        }

        try
        {
            // 首先尝试从远程同步最新信息
            await ExecuteGitCommandAsync("fetch origin", repository.LocalPath);

            // 标准化分支引用名称
            var normalizedBase = await NormalizeRefNameAsync(repository.LocalPath, @base);
            var normalizedHead = await NormalizeRefNameAsync(repository.LocalPath, head);

            _logger.LogDebug("Normalized refs: {Base} -> {NormalizedBase}, {Head} -> {NormalizedHead}", 
                @base, normalizedBase, head, normalizedHead);

            // 验证分支/引用是否存在
            var baseExists = await ValidateRefExistsAsync(repository.LocalPath, normalizedBase);
            var headExists = await ValidateRefExistsAsync(repository.LocalPath, normalizedHead);

            if (!baseExists || !headExists)
            {
                _logger.LogWarning("Invalid git references: base={Base}({BaseExists}), head={Head}({HeadExists})", 
                    normalizedBase, baseExists, normalizedHead, headExists);
                
                // 如果分支不存在，尝试从远程获取
                if (!baseExists)
                {
                    await TryFetchRemoteBranchAsync(repository.LocalPath, @base);
                    normalizedBase = await NormalizeRefNameAsync(repository.LocalPath, @base);
                }
                if (!headExists)
                {
                    await TryFetchRemoteBranchAsync(repository.LocalPath, head);
                    normalizedHead = await NormalizeRefNameAsync(repository.LocalPath, head);
                }
            }

            // 使用三点语法 base...head（显示 head 分支相对于 base 的独有更改）
            var diffTriple = await ExecuteGitCommandAsync($"diff {normalizedBase}...{normalizedHead}", repository.LocalPath);
            if (diffTriple.Success && !string.IsNullOrWhiteSpace(diffTriple.Output))
            {
                _logger.LogDebug("Successfully generated diff using three-dot syntax for {Base}...{Head}", normalizedBase, normalizedHead);
                return diffTriple.Output;
            }

            // 回退到两点语法 base..head
            var diffDouble = await ExecuteGitCommandAsync($"diff {normalizedBase}..{normalizedHead}", repository.LocalPath);
            if (diffDouble.Success && !string.IsNullOrWhiteSpace(diffDouble.Output))
            {
                _logger.LogDebug("Successfully generated diff using two-dot syntax for {Base}..{Head}", normalizedBase, normalizedHead);
                return diffDouble.Output;
            }

            // 如果仍然失败，尝试直接 diff
            var diffDirect = await ExecuteGitCommandAsync($"diff {normalizedBase} {normalizedHead}", repository.LocalPath);
            if (diffDirect.Success)
            {
                _logger.LogDebug("Successfully generated diff using direct syntax for {Base} {Head}", normalizedBase, normalizedHead);
                return diffDirect.Output;
            }

            _logger.LogWarning("All diff attempts failed for {Base} and {Head}: {Error}", normalizedBase, normalizedHead, diffDirect.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff between {Base} and {Head} for repository {RepositoryId}", 
                @base, head, repositoryId);
            return null;
        }
    }

    #endregion

    #region 仓库状态

    public async Task<bool> IsRepositoryAccessibleAsync(int repositoryId)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null) return false;

        try
        {
            var lsRemoteCommand = BuildGitLsRemoteCommand(repository);
            var result = await ExecuteGitCommandAsync(lsRemoteCommand, null);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetLatestCommitShaAsync(int repositoryId, string? branch = null)
    {
        var repository = await GetRepositoryAsync(repositoryId);
        if (repository == null || string.IsNullOrEmpty(repository.LocalPath) || !Directory.Exists(repository.LocalPath))
        {
            return null;
        }

        if (string.IsNullOrEmpty(branch))
        {
            branch = repository.DefaultBranch ?? "main";
        }

        var result = await ExecuteGitCommandAsync($"rev-parse {branch}", repository.LocalPath);
        return result.Success ? result.Output.Trim() : null;
    }

    #endregion

    #region 私有辅助方法

    private string GenerateLocalPath(GitRepository repository)
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIReview", "Repositories");
        var repoName = Path.GetFileNameWithoutExtension(repository.Url.Split('/').Last());
        var repoDir = Path.Combine(baseDir, $"{repoName}_{repository.Id}");
        return repoDir;
    }

    private string BuildGitCloneCommand(GitRepository repository)
    {
        if (!string.IsNullOrEmpty(repository.Username) && !string.IsNullOrEmpty(repository.AccessToken))
        {
            var uri = new Uri(repository.Url);
            var authenticatedUrl = $"{uri.Scheme}://{repository.Username}:{repository.AccessToken}@{uri.Host}{uri.PathAndQuery}";
            return $"clone {authenticatedUrl} \"{repository.LocalPath}\"";
        }
        
        return $"clone {repository.Url} \"{repository.LocalPath}\"";
    }

    private string BuildGitLsRemoteCommand(GitRepository repository)
    {
        if (!string.IsNullOrEmpty(repository.Username) && !string.IsNullOrEmpty(repository.AccessToken))
        {
            var uri = new Uri(repository.Url);
            var authenticatedUrl = $"{uri.Scheme}://{repository.Username}:{repository.AccessToken}@{uri.Host}{uri.PathAndQuery}";
            return $"ls-remote {authenticatedUrl}";
        }
        
        return $"ls-remote {repository.Url}";
    }

    private async Task<(bool Success, string Output, string Error)> ExecuteGitCommandAsync(string command, string? workingDirectory)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return (false, "", "Failed to start git process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return (process.ExitCode == 0, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing git command: {Command}", command);
            return (false, "", ex.Message);
        }
    }

    private List<(string Name, string CommitSha)> ParseRemoteBranches(string branchOutput)
    {
        var branches = new List<(string Name, string CommitSha)>();
        var lines = branchOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("origin/") && !trimmed.Contains("HEAD"))
            {
                var branchName = trimmed.Replace("origin/", "");
                // 这里简化处理，实际需要获取具体的commit SHA
                branches.Add((branchName, ""));
            }
        }
        
        return branches;
    }

    private List<CommitInfo> ParseCommitLog(string logOutput)
    {
        var commits = new List<CommitInfo>();
        var lines = logOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 8)
            {
                commits.Add(new CommitInfo
                {
                    Sha = parts[0],
                    AuthorName = parts[1],
                    AuthorEmail = parts[2],
                    AuthorDate = DateTime.TryParse(parts[3], out var authorDate) ? authorDate : DateTime.UtcNow,
                    CommitterName = parts[4],
                    CommitterEmail = parts[5],
                    CommitterDate = DateTime.TryParse(parts[6], out var committerDate) ? committerDate : DateTime.UtcNow,
                    Message = string.Join("|", parts.Skip(7))
                });
            }
        }
        
        return commits;
    }

    private async Task SyncFileChangesAsync(int commitId, string localPath, string sha)
    {
        try
        {
            var diffResult = await ExecuteGitCommandAsync($"show --name-status {sha}", localPath);
            if (!diffResult.Success) return;

            var lines = diffResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @"^[AMDRT]\s+"))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var changeType = parts[0] switch
                        {
                            "A" => "Added",
                            "M" => "Modified", 
                            "D" => "Deleted",
                            "R" => "Renamed",
                            "T" => "TypeChanged",
                            _ => "Modified"
                        };

                        var fileChange = new GitFileChange
                        {
                            CommitId = commitId,
                            FileName = parts[1],
                            ChangeType = changeType
                        };

                        _context.GitFileChanges.Add(fileChange);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing file changes for commit {CommitId}", commitId);
        }
    }

    private class CommitInfo
    {
        public string Sha { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public DateTime AuthorDate { get; set; }
        public string CommitterName { get; set; } = string.Empty;
        public string CommitterEmail { get; set; } = string.Empty;
        public DateTime CommitterDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private async Task<bool> ValidateRefExistsAsync(string localPath, string refName)
    {
        try
        {
            // 尝试解析引用（分支、标签或提交SHA）
            var result = await ExecuteGitCommandAsync($"rev-parse --verify {refName}", localPath);
            if (result.Success)
            {
                return true;
            }

            // 如果本地不存在，检查是否是远程分支
            var remoteResult = await ExecuteGitCommandAsync($"rev-parse --verify origin/{refName}", localPath);
            if (remoteResult.Success)
            {
                return true;
            }

            // 检查是否存在其他远程分支格式
            var remoteBranchResult = await ExecuteGitCommandAsync($"branch -r --list '*/{refName}'", localPath);
            return remoteBranchResult.Success && !string.IsNullOrWhiteSpace(remoteBranchResult.Output);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryFetchRemoteBranchAsync(string localPath, string branchName)
    {
        try
        {
            _logger.LogInformation("Attempting to fetch remote branch: {BranchName}", branchName);
            
            // 首先尝试从远程获取所有分支信息
            var fetchResult = await ExecuteGitCommandAsync("fetch origin", localPath);
            if (!fetchResult.Success)
            {
                _logger.LogWarning("Failed to fetch from origin: {Error}", fetchResult.Error);
                return false;
            }

            // 检查远程分支是否存在
            var remoteBranchCheck = await ExecuteGitCommandAsync($"branch -r --list 'origin/{branchName}'", localPath);
            if (remoteBranchCheck.Success && !string.IsNullOrWhiteSpace(remoteBranchCheck.Output))
            {
                // 如果远程分支存在，尝试创建本地分支
                var checkoutResult = await ExecuteGitCommandAsync($"checkout -b {branchName} origin/{branchName}", localPath);
                if (checkoutResult.Success)
                {
                    _logger.LogInformation("Successfully created local branch {BranchName} from origin/{BranchName}", branchName, branchName);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch remote branch: {BranchName}", branchName);
            return false;
        }
    }

    private async Task<string> NormalizeRefNameAsync(string localPath, string refName)
    {
        try
        {
            // 如果引用已经存在，直接返回
            var directResult = await ExecuteGitCommandAsync($"rev-parse --verify {refName}", localPath);
            if (directResult.Success)
            {
                return refName;
            }

            // 尝试添加 origin/ 前缀
            var originResult = await ExecuteGitCommandAsync($"rev-parse --verify origin/{refName}", localPath);
            if (originResult.Success)
            {
                return $"origin/{refName}";
            }

            // 如果都失败了，返回原始名称（让Git处理错误）
            return refName;
        }
        catch
        {
            return refName;
        }
    }

    #endregion
}