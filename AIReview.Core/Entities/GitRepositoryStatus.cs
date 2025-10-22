using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIReview.Core.Entities;

/// <summary>
/// Git 仓库拉取状态
/// </summary>
public enum GitPullStatus
{
    /// <summary>
    /// 从未拉取
    /// </summary>
    Never,
    
    /// <summary>
    /// 拉取中
    /// </summary>
    Pulling,
    
    /// <summary>
    /// 拉取成功
    /// </summary>
    Success,
    
    /// <summary>
    /// 拉取失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 部分成功（某些文件失败）
    /// </summary>
    PartialSuccess
}

/// <summary>
/// Git 仓库状态实体（按仓库维度存储拉取/同步状态）
/// </summary>
[Table("git_repository_status")]
public class GitRepositoryStatus
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 关联的仓库ID
    /// </summary>
    [Required]
    public int RepositoryId { get; set; }
    
    /// <summary>
    /// 当前分支
    /// </summary>
    [StringLength(255)]
    public string? CurrentBranch { get; set; }
    
    /// <summary>
    /// 最后拉取的 Commit Hash
    /// </summary>
    [StringLength(40)]
    public string? LastCommitHash { get; set; }
    
    /// <summary>
    /// 最后拉取时间
    /// </summary>
    public DateTime? LastPullTime { get; set; }
    
    /// <summary>
    /// 最后拉取状态
    /// </summary>
    public GitPullStatus Status { get; set; } = GitPullStatus.Never;
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 本地仓库路径
    /// </summary>
    [StringLength(500)]
    public string? LocalPath { get; set; }
    
    /// <summary>
    /// 文件总数
    /// </summary>
    public int TotalFiles { get; set; } = 0;
    
    /// <summary>
    /// 代码行数
    /// </summary>
    public long TotalLines { get; set; } = 0;
    
    /// <summary>
    /// 使用的凭证ID
    /// </summary>
    public int? GitCredentialId { get; set; }
    
    /// <summary>
    /// 是否正在拉取
    /// </summary>
    public bool IsPulling { get; set; } = false;
    
    /// <summary>
    /// 拉取进度（0-100）
    /// </summary>
    public int Progress { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 导航属性
    /// </summary>
    public virtual GitRepository Repository { get; set; } = null!;
    public virtual GitCredential? GitCredential { get; set; }
}
