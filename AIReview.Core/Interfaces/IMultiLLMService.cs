using AIReview.Core.Entities;

namespace AIReview.Core.Interfaces;

/// <summary>
/// LLM配置管理服务接口
/// </summary>
public interface ILLMConfigurationService
{
    Task<LLMConfiguration?> GetDefaultConfigurationAsync();
    Task<LLMConfiguration?> GetByIdAsync(int id);
    Task<IEnumerable<LLMConfiguration>> GetAllActiveAsync();
    Task<LLMConfiguration> CreateAsync(LLMConfiguration configuration);
    Task<LLMConfiguration> UpdateAsync(LLMConfiguration configuration);
    Task DeleteAsync(int id);
    Task SetDefaultAsync(int id);
    Task<bool> TestConnectionAsync(LLMConfiguration configuration);
}

/// <summary>
/// 多LLM提供商支持的接口
/// </summary>
public interface IMultiLLMService
{
    Task<string> GenerateReviewAsync(string code, string context, int? configurationId = null);
    Task<string> GenerateAnalysisAsync(string prompt, string code, int? configurationId = null);
    
    /// <summary>
    /// 智能代码评审:自动判断是否需要分块
    /// </summary>
    Task<string> ReviewWithAutoChunkingAsync(string diff, string context, int? configurationId = null);
    
    /// <summary>
    /// 智能AI分析:自动判断是否需要分块
    /// </summary>
    Task<string> AnalyzeWithAutoChunkingAsync(string prompt, string code, int? configurationId = null);
    
    Task<bool> TestConnectionAsync(int configurationId);
    Task<LLMConfiguration?> GetActiveConfigurationAsync();
    Task<IEnumerable<LLMConfiguration>> GetAvailableConfigurationsAsync();
}

/// <summary>
/// LLM提供商工厂接口
/// </summary>
public interface ILLMProviderFactory
{
    ILLMService CreateProvider(LLMConfiguration configuration);
    bool IsProviderSupported(string provider);
    IEnumerable<string> GetSupportedProviders();
}