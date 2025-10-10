using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// LLM提供商工厂实现
/// </summary>
public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMProviderFactory> _logger;

    private static readonly Dictionary<string, Type> SupportedProviders = new()
    {
        { "OpenAI", typeof(OpenAIService) },
        { "DeepSeek", typeof(DeepSeekService) }
    };

    public LLMProviderFactory(IServiceProvider serviceProvider, ILogger<LLMProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ILLMService CreateProvider(LLMConfiguration configuration)
    {
        if (!SupportedProviders.TryGetValue(configuration.Provider, out var providerType))
        {
            throw new NotSupportedException($"不支持的LLM提供商: {configuration.Provider}");
        }

        try
        {
            return configuration.Provider switch
            {
                "OpenAI" => CreateOpenAIService(configuration),
                "DeepSeek" => CreateDeepSeekService(configuration),
                _ => throw new NotSupportedException($"不支持的LLM提供商: {configuration.Provider}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建LLM提供商 {Provider} 时发生错误", configuration.Provider);
            throw;
        }
    }

    public bool IsProviderSupported(string provider)
    {
        return SupportedProviders.ContainsKey(provider);
    }

    public IEnumerable<string> GetSupportedProviders()
    {
        return SupportedProviders.Keys;
    }

    private ILLMService CreateOpenAIService(LLMConfiguration configuration)
    {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<OpenAIService>>();
        
        return new OpenAIService(
            httpClient,
            logger,
            configuration.ApiKey,
            configuration.Model,
            configuration.MaxTokens,
            configuration.Temperature,
            configuration.ApiEndpoint
        );
    }

    private ILLMService CreateDeepSeekService(LLMConfiguration configuration)
    {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        var logger = _serviceProvider.GetRequiredService<ILogger<DeepSeekService>>();
        
        return new DeepSeekService(
            httpClient,
            logger,
            configuration.ApiKey,
            configuration.Model,
            configuration.MaxTokens,
            configuration.Temperature,
            configuration.ApiEndpoint
        );
    }
}