using System.ComponentModel.DataAnnotations;

namespace AIReview.Core.Entities;

public class GitRepository
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? LocalPath { get; set; }
    
    [MaxLength(100)]
    public string? DefaultBranch { get; set; } = "main";
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    [MaxLength(500)]
    public string? AccessToken { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncAt { get; set; }
    
    // 关联的项目ID
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    
    // Git分支
    public ICollection<GitBranch> Branches { get; set; } = new List<GitBranch>();
    
    // Git提交
    public ICollection<GitCommit> Commits { get; set; } = new List<GitCommit>();
}

public class GitBranch
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string CommitSha { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 关联的仓库
    public int RepositoryId { get; set; }
    public GitRepository Repository { get; set; } = null!;
}

public class GitCommit
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Sha { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AuthorEmail { get; set; } = string.Empty;
    
    public DateTime AuthorDate { get; set; }
    
    [MaxLength(100)]
    public string? CommitterName { get; set; }
    
    [MaxLength(200)]
    public string? CommitterEmail { get; set; }
    
    public DateTime? CommitterDate { get; set; }
    
    [MaxLength(100)]
    public string? ParentSha { get; set; }
    
    [MaxLength(100)]
    public string? BranchName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 关联的仓库
    public int RepositoryId { get; set; }
    public GitRepository Repository { get; set; } = null!;
    
    // 关联的文件变更
    public ICollection<GitFileChange> FileChanges { get; set; } = new List<GitFileChange>();
}

public class GitFileChange
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ChangeType { get; set; } = string.Empty; // Added, Modified, Deleted, Renamed
    
    public int AddedLines { get; set; } = 0;
    
    public int DeletedLines { get; set; } = 0;
    
    public string? PatchContent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 关联的提交
    public int CommitId { get; set; }
    public GitCommit Commit { get; set; } = null!;
}