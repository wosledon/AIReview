using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

public class ContextBuilder : IContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;

    public ContextBuilder(ILogger<ContextBuilder> logger)
    {
        _logger = logger;
    }

    public Task<ReviewContext> BuildContextAsync(string diff, ReviewContext baseContext)
    {
        try
        {
            // 分析代码差异以提取更多上下文信息
            var context = new ReviewContext
            {
                Language = baseContext.Language,
                ProjectType = DetermineProjectType(diff, baseContext.Language),
                CodingStandards = GetCodingStandards(baseContext.Language)
            };

            _logger.LogDebug("Context built for language: {Language}, type: {ProjectType}", 
                context.Language, context.ProjectType);

            return Task.FromResult(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building review context");
            throw;
        }
    }

    private string DetermineProjectType(string diff, string language)
    {
        // 基于差异内容和文件路径确定项目类型
        if (language.ToLower() == "csharp")
        {
            if (diff.Contains("Controller") || diff.Contains("ApiController"))
                return "Web API";
            if (diff.Contains("DbContext") || diff.Contains("Entity"))
                return "Entity Framework";
            if (diff.Contains("xunit") || diff.Contains("Test"))
                return "Unit Test";
            return "Console Application";
        }

        if (language.ToLower() == "javascript" || language.ToLower() == "typescript")
        {
            if (diff.Contains("React") || diff.Contains("Component"))
                return "React Application";
            if (diff.Contains("express") || diff.Contains("app.listen"))
                return "Node.js Server";
            return "JavaScript Application";
        }

        return "General Application";
    }

    private string GetCodingStandards(string language)
    {
        return language.ToLower() switch
        {
            "csharp" => "Microsoft C# Coding Conventions",
            "javascript" => "Airbnb JavaScript Style Guide",
            "typescript" => "TypeScript Style Guide",
            "python" => "PEP 8 Style Guide",
            "java" => "Google Java Style Guide",
            _ => "General Best Practices"
        };
    }
}