using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIReview.Core.Entities;

/// <summary>
/// 代码变更风险评估实体
/// </summary>
[Table("risk_assessments")]
public class RiskAssessment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ReviewRequestId { get; set; }
    
    /// <summary>
    /// 整体风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double OverallRiskScore { get; set; }
    
    /// <summary>
    /// 复杂度风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double ComplexityRisk { get; set; }
    
    /// <summary>
    /// 安全风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double SecurityRisk { get; set; }
    
    /// <summary>
    /// 性能风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double PerformanceRisk { get; set; }
    
    /// <summary>
    /// 可维护性风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double MaintainabilityRisk { get; set; }
    
    /// <summary>
    /// 测试覆盖率风险评分 (0-100)
    /// </summary>
    [Range(0, 100)]
    public double TestCoverageRisk { get; set; }
    
    /// <summary>
    /// 变更范围大小 (修改的文件数量)
    /// </summary>
    public int ChangedFilesCount { get; set; }
    
    /// <summary>
    /// 变更行数
    /// </summary>
    public int ChangedLinesCount { get; set; }
    
    /// <summary>
    /// 风险评估详细说明
    /// </summary>
    public string? RiskDescription { get; set; }
    
    /// <summary>
    /// 风险缓解建议
    /// </summary>
    public string? MitigationSuggestions { get; set; }
    
    /// <summary>
    /// AI模型版本
    /// </summary>
    [StringLength(100)]
    public string? AIModelVersion { get; set; }
    
    /// <summary>
    /// 评估置信度 (0-1)
    /// </summary>
    [Range(0, 1)]
    public double? ConfidenceScore { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public virtual ReviewRequest ReviewRequest { get; set; } = null!;
}