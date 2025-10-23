using Microsoft.Extensions.Localization;
using AIReview.Core.Exceptions;
using AIReview.API.Resources;

namespace AIReview.API.Services;

/// <summary>
/// 本地化异常服务接口
/// </summary>
public interface ILocalizedExceptionService
{
    /// <summary>
    /// 创建本地化的业务异常
    /// </summary>
    BusinessException CreateLocalizedException(string errorKey, params object[] args);

    /// <summary>
    /// 获取本地化消息
    /// </summary>
    string GetLocalizedMessage(string key, params object[] args);
}

/// <summary>
/// 本地化异常服务实现
/// </summary>
public class LocalizedExceptionService : ILocalizedExceptionService
{
    private readonly IStringLocalizer _localizer;

    public LocalizedExceptionService(IStringLocalizerFactory localizerFactory)
    {
        _localizer = localizerFactory.Create(typeof(SharedResource));
    }

    public BusinessException CreateLocalizedException(string errorKey, params object[] args)
    {
        var message = GetLocalizedMessage(errorKey, args);

        return errorKey switch
        {
            "NotFound" => new NotFoundException(message),
            "Forbidden" => new ForbiddenException(message),
            "ProjectNotFound" => new NotFoundException(message),
            "ReviewNotFound" => new NotFoundException(message),
            "UserNotFound" => new NotFoundException(message),
            "RepositoryNotAccessible" => new ExternalServiceException("Git", message),
            "InvalidGitCredentials" => new ValidationException(message),
            "AnalysisInProgress" => new ConflictException(message),
            "LLMConfigurationNotFound" => new NotFoundException(message),
            _ => new ValidationException(message)
        };
    }

    public string GetLocalizedMessage(string key, params object[] args)
    {
        return args.Length > 0 ? _localizer[key, args] : _localizer[key];
    }
}