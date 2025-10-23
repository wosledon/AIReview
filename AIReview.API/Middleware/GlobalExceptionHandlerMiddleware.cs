using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AIReview.Core.Exceptions;
using AIReview.Shared.DTOs;

namespace AIReview.API.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = MapExceptionToResponse(exception);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(response);
    }

    private (int statusCode, ApiResponse<object> response) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            BusinessException businessEx => (
                businessEx.HttpStatusCode,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = businessEx.Message,
                    ErrorCode = businessEx.ErrorCode,
                    Errors = new List<string> { businessEx.Message }
                }
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "未授权访问",
                    ErrorCode = "UNAUTHORIZED",
                    Errors = new List<string> { "需要有效的身份验证" }
                }
            ),

            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "操作无效",
                    ErrorCode = "INVALID_OPERATION",
                    Errors = new List<string> { exception.Message }
                }
            ),

            ArgumentException => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "参数错误",
                    ErrorCode = "INVALID_ARGUMENT",
                    Errors = new List<string> { exception.Message }
                }
            ),

            TimeoutException => (
                StatusCodes.Status408RequestTimeout,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "请求超时",
                    ErrorCode = "TIMEOUT",
                    Errors = new List<string> { "操作超时，请稍后重试" }
                }
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "服务器内部错误",
                    ErrorCode = "INTERNAL_ERROR",
                    Errors = new List<string> { "系统繁忙，请稍后重试" }
                }
            )
        };
    }
}

/// <summary>
/// 异常处理中间件扩展方法
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}