using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIReview.Core.Interfaces;
using AIReview.Shared.DTOs;
using System.Security.Claims;

namespace AIReview.API.Controllers;

/// <summary>
/// 聊天控制器 - 提供评审专家聊天机器人功能
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILLMConfigurationService _llmConfigurationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        ILLMConfigurationService llmConfigurationService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _llmConfigurationService = llmConfigurationService;
        _logger = logger;
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendChatMessageRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var response = await _chatService.SendMessageAsync(userId, request.Content, request.ModelId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送聊天消息时发生错误");
            return StatusCode(500, "发送消息失败");
        }
    }

    /// <summary>
    /// 获取聊天历史
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetChatHistory([FromQuery] int? modelId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var history = await _chatService.GetChatHistoryAsync(userId, modelId, page, pageSize);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天历史时发生错误");
            return StatusCode(500, "获取历史记录失败");
        }
    }

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetAvailableModels()
    {
        try
        {
            var configurations = await _llmConfigurationService.GetAllActiveAsync();
            var models = configurations.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                Provider = c.Provider,
                Model = c.Model,
                IsDefault = c.IsDefault
            });

            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用模型时发生错误");
            return StatusCode(500, "获取模型列表失败");
        }
    }

    /// <summary>
    /// 清除聊天历史
    /// </summary>
    [HttpDelete("history")]
    public async Task<IActionResult> ClearChatHistory([FromQuery] int? modelId = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _chatService.ClearChatHistoryAsync(userId, modelId);
            return Ok(new { message = "聊天历史已清除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除聊天历史时发生错误");
            return StatusCode(500, "清除历史记录失败");
        }
    }
}