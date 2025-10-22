using AIReview.Core.Entities;
using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// Git 凭证服务接口
/// </summary>
public interface IGitCredentialService
{
    /// <summary>
    /// 获取用户的所有凭证
    /// </summary>
    Task<List<GitCredentialDto>> GetUserCredentialsAsync(string userId);
    
    /// <summary>
    /// 获取凭证详情
    /// </summary>
    Task<GitCredentialDto?> GetCredentialAsync(int id, string userId);
    
    /// <summary>
    /// 创建凭证
    /// </summary>
    Task<GitCredentialDto> CreateCredentialAsync(string userId, CreateGitCredentialRequest request);
    
    /// <summary>
    /// 更新凭证
    /// </summary>
    Task<GitCredentialDto> UpdateCredentialAsync(int id, string userId, UpdateGitCredentialRequest request);
    
    /// <summary>
    /// 删除凭证
    /// </summary>
    Task DeleteCredentialAsync(int id, string userId);
    
    /// <summary>
    /// 生成 SSH 密钥对
    /// </summary>
    Task<SshKeyPairDto> GenerateSshKeyPairAsync();
    
    /// <summary>
    /// 验证凭证
    /// </summary>
    Task<bool> VerifyCredentialAsync(int id, string userId, string repositoryUrl);
    
    /// <summary>
    /// 获取默认凭证
    /// </summary>
    Task<GitCredentialDto?> GetDefaultCredentialAsync(string userId);
    
    /// <summary>
    /// 设置默认凭证
    /// </summary>
    Task SetDefaultCredentialAsync(int id, string userId);
}
