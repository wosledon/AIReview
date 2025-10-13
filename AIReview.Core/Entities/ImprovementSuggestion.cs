using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

/// <summary>
/// 自动化改进建议实体
/// </summary>
[Table("improvement_suggestions")]
public class ImprovementSuggestion
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ReviewRequestId { get; set; }
    
    /// <summary>
    /// 文件路径
    /// </summary>
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    /// <summary>
    /// 起始行号
    /// </summary>
    public int? StartLine { get; set; }
    
    /// <summary>
    /// 结束行号
    /// </summary>
    public int? EndLine { get; set; }
    
    /// <summary>
    /// 建议类型
    /// </summary>
    public ImprovementType Type { get; set; }
    
    /// <summary>
    /// 建议优先级
    /// </summary>
    public ImprovementPriority Priority { get; set; }
    
    /// <summary>
    /// 建议标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 建议详细描述
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 原始代码片段
    /// </summary>
    public string? OriginalCode { get; set; }
    
    /// <summary>
    /// 建议的改进代码
    /// </summary>
    public string? SuggestedCode { get; set; }
    
    /// <summary>
    /// 改进理由
    /// </summary>
    public string? Reasoning { get; set; }
    
    /// <summary>
    /// 预期收益描述
    /// </summary>
    public string? ExpectedBenefits { get; set; }
    
    /// <summary>
    /// 实现复杂度 (1-10)
    /// </summary>
    [Range(1, 10)]
    public int ImplementationComplexity { get; set; } = 5;
    
    /// <summary>
    /// 影响范围评估
    /// </summary>
    [StringLength(500)]
    public string? ImpactAssessment { get; set; }
    
    /// <summary>
    /// 是否已被接受
    /// </summary>
    public bool IsAccepted { get; set; } = false;
    
    /// <summary>
    /// 是否已被忽略
    /// </summary>
    public bool IsIgnored { get; set; } = false;
    
    /// <summary>
    /// 用户反馈
    /// </summary>
    public string? UserFeedback { get; set; }
    
    /// <summary>
    /// AI模型版本
    /// </summary>
    [StringLength(100)]
    public string? AIModelVersion { get; set; }
    
    /// <summary>
    /// 建议置信度 (0-1)
    /// </summary>
    [Range(0, 1)]
    public double? ConfidenceScore { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ReviewRequest ReviewRequest { get; set; } = null!;
}