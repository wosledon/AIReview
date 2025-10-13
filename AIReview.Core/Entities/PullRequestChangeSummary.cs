using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AIReview.Shared.Enums;

namespace AIReview.Core.Entities;

/// <summary>
/// Pull Request 变更摘要和影响分析实体
/// </summary>
[Table("pr_change_summaries")]
public class PullRequestChangeSummary
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ReviewRequestId { get; set; }
    
    /// <summary>
    /// 变更类型
    /// </summary>
    public ChangeType ChangeType { get; set; }
    
    /// <summary>
    /// 变更摘要
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细描述
    /// </summary>
    public string? DetailedDescription { get; set; }
    
    /// <summary>
    /// 主要变更点
    /// </summary>
    public string? KeyChanges { get; set; }
    
    /// <summary>
    /// 影响范围分析
    /// </summary>
    public string? ImpactAnalysis { get; set; }
    
    /// <summary>
    /// 业务影响评估
    /// </summary>
    public BusinessImpact BusinessImpact { get; set; }
    
    /// <summary>
    /// 技术影响评估
    /// </summary>
    public TechnicalImpact TechnicalImpact { get; set; }
    
    /// <summary>
    /// 破坏性变更风险
    /// </summary>
    public BreakingChangeRisk BreakingChangeRisk { get; set; }
    
    /// <summary>
    /// 建议的测试重点
    /// </summary>
    public string? TestingRecommendations { get; set; }
    
    /// <summary>
    /// 部署注意事项
    /// </summary>
    public string? DeploymentConsiderations { get; set; }
    
    /// <summary>
    /// 依赖关系变更
    /// </summary>
    public string? DependencyChanges { get; set; }
    
    /// <summary>
    /// 性能影响评估
    /// </summary>
    public string? PerformanceImpact { get; set; }
    
    /// <summary>
    /// 安全影响评估
    /// </summary>
    public string? SecurityImpact { get; set; }
    
    /// <summary>
    /// 向后兼容性分析
    /// </summary>
    public string? BackwardCompatibility { get; set; }
    
    /// <summary>
    /// 文档更新需求
    /// </summary>
    public string? DocumentationRequirements { get; set; }
    
    /// <summary>
    /// 变更统计信息 (JSON格式)
    /// </summary>
    public string? ChangeStatistics { get; set; }
    
    /// <summary>
    /// AI模型版本
    /// </summary>
    [StringLength(100)]
    public string? AIModelVersion { get; set; }
    
    /// <summary>
    /// 分析置信度 (0-1)
    /// </summary>
    [Range(0, 1)]
    public double? ConfidenceScore { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ReviewRequest ReviewRequest { get; set; } = null!;
}