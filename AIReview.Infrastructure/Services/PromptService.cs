using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using AIReview.Shared.DTOs;
using AIReview.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIReview.Infrastructure.Services;

public class PromptService : IPromptService
{
    private readonly ApplicationDbContext _db;

    public PromptService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PromptDto> CreateAsync(CreatePromptRequest req, string currentUserId)
    {
        // 只有本人可创建自己的 User 级模板；项目模板先不做权限校验（由 Controller 层结合 ProjectService 校验）
        var entity = new PromptConfiguration
        {
            Type = req.Type,
            Name = req.Name,
            Content = req.Content,
            UserId = req.UserId,
            ProjectId = req.ProjectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PromptConfigurations.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<PromptDto> UpdateAsync(int id, UpdatePromptRequest req, string currentUserId)
    {
        var entity = await _db.PromptConfigurations.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException($"Prompt {id} 不存在");

        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name;
        if (!string.IsNullOrWhiteSpace(req.Content)) entity.Content = req.Content;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id, string currentUserId)
    {
        var entity = await _db.PromptConfigurations.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ArgumentException($"Prompt {id} 不存在");
        _db.PromptConfigurations.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<PromptDto?> GetAsync(int id)
    {
        var entity = await _db.PromptConfigurations.FirstOrDefaultAsync(x => x.Id == id);
        return entity != null ? ToDto(entity) : null;
    }

    public async Task<IEnumerable<PromptDto>> ListUserPromptsAsync(string userId)
    {
        var list = await _db.PromptConfigurations
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<PromptDto>> ListProjectPromptsAsync(int projectId)
    {
        var list = await _db.PromptConfigurations
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<PromptDto>> ListBuiltInPromptsAsync()
    {
        var list = await _db.PromptConfigurations
            .Where(p => p.UserId == null && p.ProjectId == null)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
        return list.Select(ToDto);
    }

    public async Task<EffectivePromptResponse> GetEffectivePromptAsync(PromptType type, string userId, int? projectId)
    {
        // 1) 项目级覆盖
        if (projectId.HasValue)
        {
            var projectPrompt = await _db.PromptConfigurations
                .Where(p => p.ProjectId == projectId && p.Type == type)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();
            if (projectPrompt != null)
            {
                return new EffectivePromptResponse { Type = type, Content = projectPrompt.Content, Source = "project" };
            }
        }

        // 2) 用户默认
        var userPrompt = await _db.PromptConfigurations
            .Where(p => p.UserId == userId && p.Type == type)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();
        if (userPrompt != null)
        {
            return new EffectivePromptResponse { Type = type, Content = userPrompt.Content, Source = "user" };
        }

        // 3) 内置
        return new EffectivePromptResponse
        {
            Type = type,
            Content = GetBuiltInTemplate(type),
            Source = "built-in"
        };
    }

    public static string GetBuiltInTemplate(PromptType type)
    {
        switch (type)
        {
            case PromptType.Review:
                return @"你是一位资深的代码审查专家。请基于上下文与 Git diff,对以下代码变更进行详细审查。

文件: {{FILE_NAME}}
代码变更:
{{DIFF}}

上下文:
{{CONTEXT}}

请严格输出以下 JSON 格式,不要使用 Markdown 代码块:
{
  ""summary"": ""整体评审摘要"",
  ""overallScore"": 0-100,
  ""issues"": [
    {
      ""severity"": ""high|medium|low"",
      ""category"": ""security|performance|style|bug|design|maintainability"",
      ""filePath"": ""文件路径"",
      ""line"": 行号,
      ""message"": ""问题描述"",
      ""suggestion"": ""修改建议""
    }
  ],
  ""recommendations"": [""总体改进建议""]
}

只输出 JSON,不要其他任何内容。";

            case PromptType.RiskAnalysis:
                return @"分析以下代码变更的风险等级,并提供详细的风险评估和缓解建议。

变更文件摘要:
{{FILES_SUMMARY}}

代码差异 (前1000字符):
{{DIFF_HEAD}}

请严格输出以下 JSON 格式,不要使用 Markdown 代码块:
{
  ""securityRisk"": 0-100,
  ""performanceRisk"": 0-100,
  ""riskDescription"": ""详细的风险描述"",
  ""mitigationSuggestions"": ""具体的缓解建议"",
  ""confidenceScore"": 0.0-1.0
}

只输出 JSON,不要其他任何内容。";

            case PromptType.PullRequestSummary:
                return @"请基于以下代码变更生成 Pull Request 的摘要。

变更差异:
{{DIFF}}

请严格输出以下 JSON 格式,不要使用 Markdown 代码块:
{
  ""title"": ""PR 标题"",
  ""summary"": ""总体摘要"",
  ""businessImpact"": ""业务影响描述"",
  ""technicalDetails"": ""技术细节说明"",
  ""testingRecommendations"": [""测试建议""]
}

只输出 JSON,不要其他任何内容。";

            case PromptType.ImprovementSuggestions:
                return @"请基于以下代码变更给出改进建议。

代码变更:
{{DIFF}}

上下文:
{{CONTEXT}}

请严格输出以下 JSON 格式,不要使用 Markdown 代码块:
{
  ""suggestions"": [
    {
      ""category"": ""performance|security|maintainability|design|testing"",
      ""description"": ""建议描述"",
      ""priority"": ""high|medium|low"",
      ""effort"": ""估算工作量""
    }
  ]
}

只输出 JSON,不要其他任何内容。";
        }
        return string.Empty;
    }

    private static PromptDto ToDto(PromptConfiguration e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        Name = e.Name,
        Content = e.Content,
        UserId = e.UserId,
        ProjectId = e.ProjectId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
