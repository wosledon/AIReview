using System;

namespace AIReview.Core.Exceptions;

/// <summary>
/// 业务逻辑异常基类
/// </summary>
public abstract class BusinessException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected BusinessException(string errorCode, string message, int httpStatusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    protected BusinessException(string errorCode, string message, Exception innerException, int httpStatusCode = 400)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

/// <summary>
/// 资源未找到异常
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string resourceName, object resourceId)
        : base("RESOURCE_NOT_FOUND", $"{resourceName} with id '{resourceId}' not found", 404)
    {
    }

    public NotFoundException(string message)
        : base("RESOURCE_NOT_FOUND", message, 404)
    {
    }
}

/// <summary>
/// 权限不足异常
/// </summary>
public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message = "Access denied")
        : base("ACCESS_DENIED", message, 403)
    {
    }
}

/// <summary>
/// 验证失败异常
/// </summary>
public class ValidationException : BusinessException
{
    public ValidationException(string message)
        : base("VALIDATION_FAILED", message, 400)
    {
    }

    public ValidationException(string field, string message)
        : base("VALIDATION_FAILED", $"{field}: {message}", 400)
    {
    }
}

/// <summary>
/// 冲突异常（资源已存在等）
/// </summary>
public class ConflictException : BusinessException
{
    public ConflictException(string message)
        : base("CONFLICT", message, 409)
    {
    }
}

/// <summary>
/// 外部服务异常
/// </summary>
public class ExternalServiceException : BusinessException
{
    public ExternalServiceException(string serviceName, string message)
        : base("EXTERNAL_SERVICE_ERROR", $"{serviceName}: {message}", 502)
    {
    }

    public ExternalServiceException(string serviceName, Exception innerException)
        : base("EXTERNAL_SERVICE_ERROR", $"{serviceName} service error", innerException, 502)
    {
    }
}

/// <summary>
/// Git操作异常
/// </summary>
public class GitOperationException : BusinessException
{
    public GitOperationException(string operation, string message)
        : base("GIT_OPERATION_FAILED", $"{operation}: {message}", 500)
    {
    }

    public GitOperationException(string operation, Exception innerException)
        : base("GIT_OPERATION_FAILED", $"{operation} failed", innerException, 500)
    {
    }
}

/// <summary>
/// AI服务异常
/// </summary>
public class AIServiceException : BusinessException
{
    public AIServiceException(string message)
        : base("AI_SERVICE_ERROR", message, 500)
    {
    }

    public AIServiceException(string provider, Exception innerException)
        : base("AI_SERVICE_ERROR", $"{provider} service error", innerException, 500)
    {
    }
}