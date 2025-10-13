using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// 风险评估服务接口
/// </summary>
public interface IRiskAssessmentService
{
    /// <summary>
    /// 生成风险评估
    /// </summary>
    Task<RiskAssessmentDto> GenerateRiskAssessmentAsync(int reviewRequestId);
    
    /// <summary>
    /// 获取风险评估
    /// </summary>
    Task<RiskAssessmentDto?> GetRiskAssessmentAsync(int reviewRequestId);
    
    /// <summary>
    /// 更新风险评估
    /// </summary>
    Task<RiskAssessmentDto> UpdateRiskAssessmentAsync(int id, RiskAssessmentDto dto);
}