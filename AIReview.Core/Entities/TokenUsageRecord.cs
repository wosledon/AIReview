using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIReview.Core.Entities;

/// <summary>
/// Token使用记录实体
/// 用于追踪每次LLM调用的token消耗和成本
/// </summary>
[Table("token_usage_records")]
public class TokenUsageRecord
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的项目ID(可选)
    /// </summary>
    public int? ProjectId { get; set; }
    
    /// <summary>
    /// 关联的评审请求ID(可选)
    /// </summary>
    public int? ReviewRequestId { get; set; }
    
    /// <summary>
    /// LLM配置ID(可选,如果配置已删除或未设置)
    /// </summary>
    public int? LLMConfigurationId { get; set; }
    
    /// <summary>
    /// 提供商名称(OpenAI, DeepSeek等)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// 模型名称(gpt-4, deepseek-coder等)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型(Review, RiskAnalysis, PullRequestSummary, ImprovementSuggestions)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 输入Token数
    /// </summary>
    [Required]
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// 输出Token数
    /// </summary>
    [Required]
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// 总Token数
    /// </summary>
    [Required]
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// 输入成本(美元)
    /// </summary>
    [Column(TypeName = "decimal(18, 8)")]
    public decimal PromptCost { get; set; }
    
    /// <summary>
    /// 输出成本(美元)
    /// </summary>
    [Column(TypeName = "decimal(18, 8)")]
    public decimal CompletionCost { get; set; }
    
    /// <summary>
    /// 总成本(美元)
    /// </summary>
    [Column(TypeName = "decimal(18, 8)")]
    public decimal TotalCost { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessful { get; set; } = true;
    
    /// <summary>
    /// 错误信息(如果失败)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 响应时间(毫秒)
    /// </summary>
    public int? ResponseTimeMs { get; set; }
    
    /// <summary>
    /// 是否使用缓存
    /// </summary>
    public bool IsCached { get; set; } = false;
    
    /// <summary>
    /// 记录创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual ReviewRequest? ReviewRequest { get; set; }
    public virtual LLMConfiguration LLMConfiguration { get; set; } = null!;
}
