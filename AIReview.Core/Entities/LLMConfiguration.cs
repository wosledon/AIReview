using System.ComponentModel.DataAnnotations;

namespace AIReview.Core.Entities;

/// <summary>
/// LLM配置实体
/// </summary>
public class LLMConfiguration
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // OpenAI, DeepSeek, etc.
    
    [Required]
    [MaxLength(200)]
    public string ApiEndpoint { get; set; } = string.Empty;
    
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;
    
    public int MaxTokens { get; set; } = 4000;
    
    public double Temperature { get; set; } = 0.3;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 额外配置参数（JSON格式）
    /// </summary>
    public string? ExtraParameters { get; set; }
}