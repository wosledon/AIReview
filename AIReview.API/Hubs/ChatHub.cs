using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using System.Security.Claims;

namespace AIReview.API.Hubs;

/// <summary>
/// 聊天Hub - 处理实时聊天消息
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessage(string content, int modelId)
    {
        try
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "用户未认证");
                return;
            }

            // 发送用户消息
            var userMessage = new ChatMessageDto
            {
                UserId = userId,
                Content = content,
                Type = ChatMessageType.User,
                ModelId = modelId
            };

            await Clients.Caller.SendAsync("ReceiveMessage", userMessage);

            // 获取机器人回复
            var botResponse = await _chatService.SendMessageAsync(userId, content, modelId);

            // 发送机器人回复
            var botMessage = new ChatMessageDto
            {
                UserId = userId,
                Content = botResponse.Content,
                Type = ChatMessageType.Bot,
                ModelId = modelId
            };

            await Clients.Caller.SendAsync("ReceiveMessage", botMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送聊天消息时发生错误，用户: {UserId}", Context.UserIdentifier);
            await Clients.Caller.SendAsync("Error", "发送消息失败");
        }
    }

    /// <summary>
    /// 加入聊天组
    /// </summary>
    public async Task JoinChat()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{userId}");
            await Clients.Caller.SendAsync("JoinedChat", userId);
        }
    }

    /// <summary>
    /// 离开聊天组
    /// </summary>
    public async Task LeaveChat()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{userId}");
            await Clients.Caller.SendAsync("LeftChat", userId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("用户 {UserId} 连接到聊天Hub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("用户 {UserId} 断开聊天Hub连接", userId);
        await base.OnDisconnectedAsync(exception);
    }
}