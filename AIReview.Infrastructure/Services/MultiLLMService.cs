using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// 多LLM服务实现
/// </summary>
public class MultiLLMService : IMultiLLMService
{
    private readonly ILLMConfigurationService _configurationService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ChunkedReviewService _chunkedReviewService;
    private readonly ILogger<MultiLLMService> _logger;

    public MultiLLMService(
        ILLMConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        ChunkedReviewService chunkedReviewService,
        ILogger<MultiLLMService> logger)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
        _chunkedReviewService = chunkedReviewService;
        _logger = logger;
    }

    public async Task<string> GenerateReviewAsync(string code, string context, int? configurationId = null)
    {
        var configuration = await GetConfigurationAsync(configurationId);
        if (configuration == null)
        {
            throw new InvalidOperationException("没有可用的LLM配置");
        }

        try
        {
            var provider = _providerFactory.CreateProvider(configuration);
            
            var prompt = BuildReviewPrompt(code, context);
            
            _logger.LogInformation("使用 {Provider} 进行代码审查, Prompt长度: {PromptLength}", 
                configuration.Provider, prompt.Length);

            var result = await provider.GenerateAsync(prompt);
            
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException($"LLM提供商 {configuration.Provider} 返回了空结果");
            }
            
            _logger.LogInformation("使用 {Provider} 完成代码审查生成, 结果长度: {ResultLength}", 
                configuration.Provider, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用 {Provider} 生成代码审查时发生错误", configuration.Provider);
            throw;
        }
    }

    /// <summary>
    /// 构建结构化的代码评审Prompt
    /// </summary>
    private string BuildReviewPrompt(string code, string context)
    {
        return $@"# 代码审查任务

你是一位资深的代码审查专家。请仔细分析以下Git差异，提供专业、详细的审查报告。

## 上下文信息
{context}

## Git差异内容
```diff
{code}
```

## 审查要求

请从以下维度进行全面分析：

### 1. 代码质量 (Code Quality)
- 代码的可读性和清晰度
- 变量命名和函数命名是否符合规范
- 代码结构和组织是否合理
- 是否遵循SOLID原则

### 2. 潜在问题 (Potential Issues)
- 逻辑错误或边界条件处理不当
- 可能的空引用或未处理的异常
- 资源泄漏风险(文件句柄、数据库连接等)
- 并发安全问题

### 3. 安全性 (Security)
- SQL注入、XSS等常见安全漏洞
- 敏感信息泄露风险
- 输入验证和数据清理
- 权限控制和访问验证

### 4. 性能 (Performance)
- 算法复杂度和效率
- 数据库查询优化机会
- 缓存使用策略
- 内存使用和垃圾回收

### 5. 最佳实践 (Best Practices)
- 是否符合语言和框架的最佳实践
- 设计模式的应用
- 错误处理和日志记录
- 测试覆盖率考虑

## 输出格式要求（严格）

仅输出严格的 JSON（UTF-8），不得包含 Markdown 代码块（如 ```json 或 ```）、注释或任何额外文字。输出的 JSON 顶层必须是对象。

建议的字段结构如下（示例，仅供理解，用于说明字段含义；不要在输出中包含注释或示例说明）：
```json
{{
    ""summary"": ""总体评价，简要描述本次变更的质量和主要发现"",
    ""overallScore"": 85,
    ""issues"": [
        {{
            ""severity"": ""high|medium|low"",
            ""category"": ""security|performance|style|bug|design|maintainability"",
            ""filePath"": ""从Git diff中提取的完整文件路径"",
            ""line"": 123,
            ""message"": ""问题描述，要具体明确"",
            ""suggestion"": ""具体的改进建议或修复方案""
        }}
    ],
    ""recommendations"": [
        ""总体性的改进建议1"",
        ""总体性的改进建议2""
    ]
}}
```

## 重要提示

1. **准确的位置信息**: 必须从Git diff中准确提取`filePath`和`line`信息
   - 文件路径格式: `diff --git a/path/to/file.ext b/path/to/file.ext`
   - 行号信息: `@@ -旧行号,行数 +新行号,行数 @@`

2. **严重程度分级**:
   - `high`: 严重bug、安全漏洞、性能问题
   - `medium`: 警告、潜在问题、不当实践
   - `low`: 建议、优化、风格问题

3. **类别分类**:
   - `security`: 安全相关
   - `performance`: 性能相关
   - `bug`: 功能缺陷
   - `style`: 代码风格
   - `design`: 设计和架构
   - `maintainability`: 可维护性

4. **评分标准** (0-100分):
   - 90-100: 优秀，几乎无问题
   - 75-89: 良好，有少量可改进点
   - 60-74: 中等，有明显问题需要修复
   - 0-59: 较差，有严重问题

5. 只返回 JSON，不要添加任何其他说明文字或 Markdown 代码块
6. 确保 JSON 语法正确（双引号、无尾逗号），可以被解析器正确解析
7. 如果没有发现问题，issues 数组可以为空，但请给出建设性的 recommendations";
    }

    public async Task<string> GenerateAnalysisAsync(string prompt, string code, int? configurationId = null)
    {
        var configuration = await GetConfigurationAsync(configurationId);
        if (configuration == null)
        {
            throw new InvalidOperationException("未找到可用的LLM配置");
        }

        try
        {
            var provider = _providerFactory.CreateProvider(configuration);
            
            // 构建完整的分析prompt
            var fullPrompt = BuildAnalysisPrompt(prompt, code);

            _logger.LogInformation("使用 {Provider} 进行AI分析, Prompt长度: {PromptLength}", 
                configuration.Provider, fullPrompt.Length);
            
            var result = await provider.GenerateAsync(fullPrompt);
            
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException($"LLM提供商 {configuration.Provider} 返回了空结果");
            }
            
            _logger.LogInformation("使用 {Provider} 完成AI分析, 结果长度: {ResultLength}", 
                configuration.Provider, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用 {Provider} 进行AI分析时发生错误", configuration.Provider);
            throw;
        }
    }

    /// <summary>
    /// 构建分析任务的Prompt
    /// </summary>
    private string BuildAnalysisPrompt(string taskPrompt, string code)
    {
        return $@"# AI 分析任务

{taskPrompt}

## 代码内容
```
{code}
```

## 输出要求（严格）

1. 仅输出严格的 JSON（UTF-8），不得包含 Markdown 代码块（如 ```json 或 ```）、注释或任何额外文字
2. 顶层结构与任务约定保持一致（通常为对象）；字段名使用双引号，避免尾逗号
3. 确保分析深入、全面、准确，并给出可验证的依据
4. 如果发现问题，请提供可行且具体的解决方案
5. 如无问题，也需要给出改进建议数组（可为空数组）

请直接输出 JSON。";
    }

    public async Task<bool> TestConnectionAsync(int configurationId)
    {
        var configuration = await _configurationService.GetByIdAsync(configurationId);
        if (configuration == null)
        {
            return false;
        }

        return await _configurationService.TestConnectionAsync(configuration);
    }

    public async Task<string> ReviewWithAutoChunkingAsync(string diff, string context, int? configurationId = null)
    {
        return await _chunkedReviewService.ReviewWithAutoChunkingAsync(diff, context, configurationId);
    }

    public async Task<string> AnalyzeWithAutoChunkingAsync(string prompt, string code, int? configurationId = null)
    {
        return await _chunkedReviewService.AnalyzeWithAutoChunkingAsync(prompt, code, configurationId);
    }

    public async Task<LLMConfiguration?> GetActiveConfigurationAsync()
    {
        return await _configurationService.GetDefaultConfigurationAsync();
    }

    public async Task<IEnumerable<LLMConfiguration>> GetAvailableConfigurationsAsync()
    {
        return await _configurationService.GetAllActiveAsync();
    }

    private async Task<LLMConfiguration?> GetConfigurationAsync(int? configurationId)
    {
        if (configurationId.HasValue)
        {
            var config = await _configurationService.GetByIdAsync(configurationId.Value);
            if (config != null && config.IsActive)
            {
                return config;
            }
        }

        // 如果没有指定ID或指定的配置不可用，使用默认配置
        return await _configurationService.GetDefaultConfigurationAsync();
    }
}