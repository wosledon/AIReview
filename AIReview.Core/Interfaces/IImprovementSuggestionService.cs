using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// 改进建议服务接口
/// </summary>
public interface IImprovementSuggestionService
{
    /// <summary>
    /// 生成改进建议
    /// </summary>
    Task<List<ImprovementSuggestionDto>> GenerateImprovementSuggestionsAsync(int reviewRequestId);
    
    /// <summary>
    /// 获取评审的所有改进建议
    /// </summary>
    Task<List<ImprovementSuggestionDto>> GetImprovementSuggestionsAsync(int reviewRequestId);
    
    /// <summary>
    /// 获取单个改进建议
    /// </summary>
    Task<ImprovementSuggestionDto?> GetImprovementSuggestionAsync(int suggestionId);
    
    /// <summary>
    /// 更新改进建议状态
    /// </summary>
    Task<ImprovementSuggestionDto> UpdateImprovementSuggestionAsync(int suggestionId, UpdateImprovementSuggestionRequest request);
    
    /// <summary>
    /// 批量更新改进建议状态
    /// </summary>
    Task<List<ImprovementSuggestionDto>> BulkUpdateImprovementSuggestionsAsync(List<int> suggestionIds, UpdateImprovementSuggestionRequest request);
}