using AIReview.Core.Interfaces;
using AIReview.Core.Entities;
using AIReview.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// 聊天服务实现
/// </summary>
public class ChatService : IChatService
{
    private readonly ILLMConfigurationService _configurationService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<ChatService> _logger;
    private readonly IDistributedCacheService _cacheService;

    // 评审专家系统提示
    private const string REVIEW_EXPERT_PROMPT = @"
你是一个专业的代码评审专家，专注于提供高质量的代码审查建议。

你的职责包括：
1. 识别代码中的潜在问题、安全漏洞和最佳实践违规
2. 提供具体的改进建议和代码示例
3. 解释技术决策的理由
4. 帮助开发者提高代码质量和可维护性

请始终保持专业、建设性和帮助性的态度。在回复时：
- 提供具体的、可操作的建议
- 解释为什么某个做法是问题或改进
- 如果合适，提供代码示例
- 保持简洁但信息丰富

记住，你是评审专家，所以要专注于代码质量、技术债务和最佳实践等方面。";

    public ChatService(
        ILLMConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        ILogger<ChatService> logger,
        IDistributedCacheService cacheService)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ChatMessageDto> SendMessageAsync(string userId, string message, int modelId)
    {
        try
        {
            // 获取LLM配置
            var configuration = await _configurationService.GetByIdAsync(modelId);
            if (configuration == null)
            {
                throw new InvalidOperationException($"找不到ID为{modelId}的LLM配置");
            }

            // 创建LLM提供商
            var provider = _providerFactory.CreateProvider(configuration);

            // 构建评审专家的上下文
            var contextMessage = $"{REVIEW_EXPERT_PROMPT}\n\n用户问题：{message}";

            _logger.LogInformation("使用 {Provider} 进行聊天回复, Prompt长度: {PromptLength}",
                configuration.Provider, contextMessage.Length);

            // 调用LLM服务获取回复
            var response = await provider.GenerateAsync(contextMessage);

            var botMessage = new ChatMessageDto
            {
                UserId = userId,
                Content = response,
                Type = ChatMessageType.Bot,
                ModelId = modelId
            };

            // 异步保存聊天历史（可以考虑使用队列或后台任务）
            await SaveChatHistoryAsync(userId, message, response, modelId);

            return botMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送聊天消息失败，用户: {UserId}, 模型: {ModelId}", userId, modelId);

            return new ChatMessageDto
            {
                UserId = userId,
                Content = "抱歉，我现在无法回复。请稍后再试。",
                Type = ChatMessageType.Bot,
                ModelId = modelId
            };
        }
    }

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(string userId, int? modelId = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var cacheKey = $"chat_history_{userId}_{modelId ?? 0}_{page}_{pageSize}";
            var cachedHistory = await _cacheService.GetAsync<List<ChatMessageDto>>(cacheKey);

            if (cachedHistory != null)
            {
                return cachedHistory;
            }

            // 这里应该从数据库获取历史记录
            // 暂时返回空列表，实际实现需要数据库表
            var history = new List<ChatMessageDto>();

            // 缓存结果
            await _cacheService.SetAsync(cacheKey, history, TimeSpan.FromMinutes(5));

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天历史失败，用户: {UserId}", userId);
            return new List<ChatMessageDto>();
        }
    }

    public async Task ClearChatHistoryAsync(string userId, int? modelId = null)
    {
        try
        {
            // 这里应该从数据库删除历史记录
            // 暂时只清除缓存
            var cacheKeyPattern = $"chat_history_{userId}_{modelId ?? 0}_*";
            // 实际实现需要清除匹配的缓存键

            _logger.LogInformation("清除用户 {UserId} 的聊天历史", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除聊天历史失败，用户: {UserId}", userId);
            throw;
        }
    }

    private async Task SaveChatHistoryAsync(string userId, string userMessage, string botResponse, int modelId)
    {
        try
        {
            // 这里应该保存到数据库
            // 暂时只记录日志
            _logger.LogInformation("保存聊天记录 - 用户: {UserId}, 模型: {ModelId}", userId, modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存聊天历史失败，用户: {UserId}", userId);
        }
    }
}