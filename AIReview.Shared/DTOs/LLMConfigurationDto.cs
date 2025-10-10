using System.ComponentModel.DataAnnotations;

namespace AIReview.Shared.DTOs;

/// <summary>
/// LLM配置DTO
/// </summary>
public class LLMConfigurationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ExtraParameters { get; set; }
}

/// <summary>
/// 创建LLM配置DTO
/// </summary>
public class CreateLLMConfigurationDto
{
    [Required(ErrorMessage = "配置名称不能为空")]
    [MaxLength(100, ErrorMessage = "配置名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "提供商不能为空")]
    [MaxLength(50, ErrorMessage = "提供商名称长度不能超过50个字符")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "API端点不能为空")]
    [MaxLength(200, ErrorMessage = "API端点长度不能超过200个字符")]
    [Url(ErrorMessage = "请输入有效的URL")]
    public string ApiEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "API密钥不能为空")]
    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "模型名称长度不能超过100个字符")]
    public string Model { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "最大令牌数必须在1-100000之间")]
    public int MaxTokens { get; set; } = 4000;

    [Range(0.0, 2.0, ErrorMessage = "温度值必须在0.0-2.0之间")]
    public double Temperature { get; set; } = 0.3;

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; } = false;

    public string? ExtraParameters { get; set; }
}

/// <summary>
/// 更新LLM配置DTO
/// </summary>
public class UpdateLLMConfigurationDto
{
    [Required(ErrorMessage = "配置名称不能为空")]
    [MaxLength(100, ErrorMessage = "配置名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "提供商不能为空")]
    [MaxLength(50, ErrorMessage = "提供商名称长度不能超过50个字符")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "API端点不能为空")]
    [MaxLength(200, ErrorMessage = "API端点长度不能超过200个字符")]
    [Url(ErrorMessage = "请输入有效的URL")]
    public string ApiEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "API密钥不能为空")]
    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "模型名称长度不能超过100个字符")]
    public string Model { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "最大令牌数必须在1-100000之间")]
    public int MaxTokens { get; set; } = 4000;

    [Range(0.0, 2.0, ErrorMessage = "温度值必须在0.0-2.0之间")]
    public double Temperature { get; set; } = 0.3;

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; } = false;

    public string? ExtraParameters { get; set; }
}

/// <summary>
/// 连接测试结果DTO
/// </summary>
public class TestConnectionResultDto
{
    public bool IsConnected { get; set; }
    public string Message { get; set; } = string.Empty;
}