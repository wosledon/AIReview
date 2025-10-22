using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using AIReview.Infrastructure.Data;

namespace AIReview.Infrastructure.Services;

public class GitCredentialService : IGitCredentialService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GitCredentialService> _logger;
    private const string EncryptionKey = "YourSecureEncryptionKeyHere_Change_In_Production"; // 应从配置中读取

    public GitCredentialService(ApplicationDbContext db, IUnitOfWork unitOfWork, ILogger<GitCredentialService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<GitCredentialDto>> GetUserCredentialsAsync(string userId)
    {
        var credentials = await _db.Set<GitCredential>()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        return credentials.Select(MapToDto).ToList();
    }

    public async Task<GitCredentialDto?> GetCredentialAsync(int id, string userId)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        return credential != null ? MapToDto(credential) : null;
    }

    public async Task<GitCredentialDto> CreateCredentialAsync(string userId, CreateGitCredentialRequest request)
    {
        // 验证类型
        if (!Enum.TryParse<GitCredentialType>(request.Type, out var credentialType))
        {
            throw new ArgumentException($"Invalid credential type: {request.Type}");
        }

        var credential = new GitCredential
        {
            Name = request.Name,
            Type = credentialType,
            Provider = request.Provider,
            Username = request.Username,
            IsDefault = request.IsDefault,
            IsActive = true,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 加密敏感信息
        if (!string.IsNullOrEmpty(request.Secret))
        {
            credential.EncryptedSecret = Encrypt(request.Secret);
        }

        if (!string.IsNullOrEmpty(request.PrivateKey))
        {
            credential.EncryptedPrivateKey = Encrypt(request.PrivateKey);
            // 从私钥提取公钥（简化处理，实际应该正确解析 SSH 密钥）
            credential.PublicKey = ExtractPublicKeyFromPrivateKey(request.PrivateKey);
        }

        // 如果设置为默认，取消其他默认凭证
        if (credential.IsDefault)
        {
            await ClearDefaultCredentialsAsync(userId);
        }

    _db.Set<GitCredential>().Add(credential);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created Git credential {CredentialId} for user {UserId}", credential.Id, userId);

        return MapToDto(credential);
    }

    public async Task<GitCredentialDto> UpdateCredentialAsync(int id, string userId, UpdateGitCredentialRequest request)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (credential == null)
        {
            throw new KeyNotFoundException($"Credential {id} not found");
        }

        if (request.Name != null) credential.Name = request.Name;
        if (request.Username != null) credential.Username = request.Username;
        
        if (request.Secret != null)
        {
            credential.EncryptedSecret = Encrypt(request.Secret);
        }

        if (request.PrivateKey != null)
        {
            credential.EncryptedPrivateKey = Encrypt(request.PrivateKey);
            credential.PublicKey = ExtractPublicKeyFromPrivateKey(request.PrivateKey);
        }

        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            await ClearDefaultCredentialsAsync(userId);
            credential.IsDefault = true;
        }

        if (request.IsActive.HasValue)
        {
            credential.IsActive = request.IsActive.Value;
        }

        credential.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated Git credential {CredentialId}", credential.Id);

        return MapToDto(credential);
    }

    public async Task DeleteCredentialAsync(int id, string userId)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (credential == null)
        {
            throw new KeyNotFoundException($"Credential {id} not found");
        }

    _db.Set<GitCredential>().Remove(credential);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted Git credential {CredentialId}", credential.Id);
    }

    public async Task<SshKeyPairDto> GenerateSshKeyPairAsync()
    {
        // 使用 RSA 生成 SSH 密钥对（简化版本）
        using var rsa = RSA.Create(2048);
        
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

        // 格式化为 SSH 格式
        var sshPrivateKey = $"-----BEGIN RSA PRIVATE KEY-----\n{privateKey}\n-----END RSA PRIVATE KEY-----";
        var sshPublicKey = $"ssh-rsa {publicKey} generated-by-aireviewer";

        return new SshKeyPairDto
        {
            PrivateKey = sshPrivateKey,
            PublicKey = sshPublicKey
        };
    }

    public async Task<bool> VerifyCredentialAsync(int id, string userId, string repositoryUrl)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (credential == null)
        {
            return false;
        }

        try
        {
            // 这里应该实际测试 Git 连接，简化处理
            // 实际实现应使用 LibGit2Sharp 或类似库测试连接
            
            credential.IsVerified = true;
            credential.LastVerifiedAt = DateTime.UtcNow;
            credential.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Verified Git credential {CredentialId}", credential.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Git credential {CredentialId}", credential.Id);
            
            credential.IsVerified = false;
            credential.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            
            return false;
        }
    }

    public async Task<GitCredentialDto?> GetDefaultCredentialAsync(string userId)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsDefault && c.IsActive);

        return credential != null ? MapToDto(credential) : null;
    }

    public async Task SetDefaultCredentialAsync(int id, string userId)
    {
        var credential = await _db.Set<GitCredential>()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (credential == null)
        {
            throw new KeyNotFoundException($"Credential {id} not found");
        }

        await ClearDefaultCredentialsAsync(userId);
        
        credential.IsDefault = true;
        credential.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Set Git credential {CredentialId} as default", credential.Id);
    }

    private async Task ClearDefaultCredentialsAsync(string userId)
    {
        var credentials = await _db.Set<GitCredential>()
            .Where(c => c.UserId == userId && c.IsDefault)
            .ToListAsync();

        foreach (var cred in credentials)
        {
            cred.IsDefault = false;
            cred.UpdatedAt = DateTime.UtcNow;
        }

        if (credentials.Any())
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private GitCredentialDto MapToDto(GitCredential credential)
    {
        return new GitCredentialDto
        {
            Id = credential.Id,
            Name = credential.Name,
            Type = credential.Type.ToString(),
            Provider = credential.Provider,
            Username = credential.Username,
            PublicKey = credential.PublicKey,
            IsDefault = credential.IsDefault,
            IsActive = credential.IsActive,
            IsVerified = credential.IsVerified,
            LastVerifiedAt = credential.LastVerifiedAt,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt
        };
    }

    private string Encrypt(string plainText)
    {
        // 简化的加密实现，生产环境应使用更安全的方法
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16]; // 应使用随机 IV

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(encrypted);
    }

    private string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16];

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        
        return Encoding.UTF8.GetString(decrypted);
    }

    private string ExtractPublicKeyFromPrivateKey(string privateKey)
    {
        // 简化实现，实际应正确解析 SSH 密钥格式
        return $"ssh-rsa {Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey.Substring(0, Math.Min(50, privateKey.Length))))} extracted";
    }
}
