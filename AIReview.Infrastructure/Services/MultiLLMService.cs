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
    private readonly ILogger<MultiLLMService> _logger;

    public MultiLLMService(
        ILLMConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        ILogger<MultiLLMService> logger)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
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
            
            var prompt = $@"作为一名资深的代码审查专家，请仔细分析以下代码并提供详细的审查报告。

上下文信息：
{context}

待审查代码：
```
{code}
```

请从以下几个方面进行分析：
1. 代码质量和可读性
2. 潜在的安全问题
3. 性能优化建议
4. 最佳实践遵循情况
5. 可能的bug或逻辑错误

请以JSON格式返回审查结果，包含以下字段：
- summary: 总体评价
- issues: 发现的问题列表（每个问题包含severity, line, message, suggestion）
- score: 代码质量评分（1-10分）
- recommendations: 改进建议";

            var result = await provider.GenerateAsync(prompt);
            
            _logger.LogInformation("使用 {Provider} 完成代码审查生成", configuration.Provider);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用 {Provider} 生成代码审查时发生错误", configuration.Provider);
            throw;
        }
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