using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIReview.Core.Entities;

/// <summary>
/// Git 凭证类型
/// </summary>
public enum GitCredentialType
{
    /// <summary>
    /// SSH 密钥
    /// </summary>
    SSH,
    
    /// <summary>
    /// Personal Access Token
    /// </summary>
    Token,
    
    /// <summary>
    /// 用户名密码
    /// </summary>
    UsernamePassword
}

/// <summary>
/// Git 凭证实体
/// </summary>
[Table("git_credentials")]
public class GitCredential
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 凭证名称
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 凭证类型
    /// </summary>
    [Required]
    public GitCredentialType Type { get; set; }
    
    /// <summary>
    /// Git 服务商（GitHub, GitLab, Gitee 等）
    /// </summary>
    [StringLength(50)]
    public string? Provider { get; set; }
    
    /// <summary>
    /// 用户名（用于 Token 和 UsernamePassword）
    /// </summary>
    [StringLength(255)]
    public string? Username { get; set; }
    
    /// <summary>
    /// 密码或 Token（加密存储）
    /// </summary>
    public string? EncryptedSecret { get; set; }
    
    /// <summary>
    /// SSH 私钥（加密存储）
    /// </summary>
    public string? EncryptedPrivateKey { get; set; }
    
    /// <summary>
    /// SSH 公钥
    /// </summary>
    public string? PublicKey { get; set; }
    
    /// <summary>
    /// 是否为默认凭证
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 最后验证时间
    /// </summary>
    public DateTime? LastVerifiedAt { get; set; }
    
    /// <summary>
    /// 验证状态
    /// </summary>
    public bool IsVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 导航属性
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
}
