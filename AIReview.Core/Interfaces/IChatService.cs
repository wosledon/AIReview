using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

/// <summary>
/// 聊天服务接口
/// </summary>
public interface IChatService
{
    /// <summary>
    /// 发送聊天消息并获取回复
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="message">消息内容</param>
    /// <param name="modelId">使用的模型ID</param>
    /// <returns>机器人回复</returns>
    Task<ChatMessageDto> SendMessageAsync(string userId, string message, int modelId);

    /// <summary>
    /// 获取聊天历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="modelId">模型ID（可选）</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>聊天历史列表</returns>
    Task<List<ChatMessageDto>> GetChatHistoryAsync(string userId, int? modelId = null, int page = 1, int pageSize = 50);

    /// <summary>
    /// 清除聊天历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="modelId">模型ID（可选，为null时清除所有）</param>
    Task ClearChatHistoryAsync(string userId, int? modelId = null);
}