namespace AIReview.Shared.DTOs;

/// <summary>
/// Git 凭证 DTO
/// </summary>
public class GitCredentialDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? Username { get; set; }
    public string? PublicKey { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建 Git 凭证请求
/// </summary>
public class CreateGitCredentialRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "SSH", "Token", "UsernamePassword"
    public string? Provider { get; set; }
    public string? Username { get; set; }
    public string? Secret { get; set; } // Token or Password
    public string? PrivateKey { get; set; } // For SSH
    public bool IsDefault { get; set; }
}

/// <summary>
/// 更新 Git 凭证请求
/// </summary>
public class UpdateGitCredentialRequest
{
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Secret { get; set; }
    public string? PrivateKey { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// SSH 密钥对
/// </summary>
public class SshKeyPairDto
{
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}

/// <summary>
/// Git 仓库状态 DTO
/// </summary>
public class GitRepositoryStatusDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? CurrentBranch { get; set; }
    public string? LastCommitHash { get; set; }
    public DateTime? LastPullTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? LocalPath { get; set; }
    public int TotalFiles { get; set; }
    public long TotalLines { get; set; }
    public int? GitCredentialId { get; set; }
    public string? GitCredentialName { get; set; }
    public bool IsPulling { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 拉取仓库请求
/// </summary>
public class PullRepositoryRequest
{
    public int ProjectId { get; set; }
    public int? GitCredentialId { get; set; }
    public string? Branch { get; set; }
    public bool Force { get; set; } = false;
}
