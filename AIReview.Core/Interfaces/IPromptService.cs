using AIReview.Shared.Enums;
using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

public interface IPromptService
{
    // CRUD
    Task<PromptDto> CreateAsync(CreatePromptRequest req, string currentUserId);
    Task<PromptDto> UpdateAsync(int id, UpdatePromptRequest req, string currentUserId);
    Task DeleteAsync(int id, string currentUserId);
    Task<PromptDto?> GetAsync(int id);
    Task<IEnumerable<PromptDto>> ListUserPromptsAsync(string userId);
    Task<IEnumerable<PromptDto>> ListProjectPromptsAsync(int projectId);
    Task<IEnumerable<PromptDto>> ListBuiltInPromptsAsync();

    // 解析最终生效模板(项目优先,回退用户默认,再回退内置)
    Task<EffectivePromptResponse> GetEffectivePromptAsync(PromptType type, string userId, int? projectId);
}
