using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using AIReview.API.Services;

namespace AIReview.API.Controllers;

/// <summary>
/// 多语言测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocalizationController : ControllerBase
{
    private readonly IStringLocalizer _localizer;
    private readonly ILocalizedExceptionService _exceptionService;

    public LocalizationController(
        IStringLocalizerFactory localizerFactory,
        ILocalizedExceptionService exceptionService)
    {
        _localizer = localizerFactory.Create(typeof(Resources.SharedResource));
        _exceptionService = exceptionService;
    }

    /// <summary>
    /// 获取本地化消息
    /// </summary>
    [HttpGet("message")]
    public IActionResult GetMessage(string key = "OperationSuccessful")
    {
        var message = _localizer[key];
        return Ok(new
        {
            Key = key,
            Message = message.Value,
            Culture = Thread.CurrentThread.CurrentCulture.Name,
            UICulture = Thread.CurrentThread.CurrentUICulture.Name
        });
    }

    /// <summary>
    /// 测试异常本地化
    /// </summary>
    [HttpGet("error")]
    public IActionResult GetError(string errorKey = "NotFound")
    {
        throw _exceptionService.CreateLocalizedException(errorKey);
    }

    /// <summary>
    /// 获取所有支持的文化
    /// </summary>
    [HttpGet("cultures")]
    public IActionResult GetSupportedCultures()
    {
        var cultures = new[]
        {
            new { Name = "en-US", DisplayName = "English (US)" },
            new { Name = "zh-CN", DisplayName = "中文(简体)" }
        };

        return Ok(cultures);
    }
}