namespace AIReview.Core.Entities;

/// <summary>
/// LLM提供商的定价配置
/// 用于计算token成本
/// </summary>
public class LLMPricingConfig
{
    /// <summary>
    /// 提供商名称
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 输入token价格(每百万token的美元价格)
    /// </summary>
    public decimal PromptPricePerMillionTokens { get; set; }
    
    /// <summary>
    /// 输出token价格(每百万token的美元价格)
    /// </summary>
    public decimal CompletionPricePerMillionTokens { get; set; }
    
    /// <summary>
    /// 货币单位(默认USD)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LLM定价配置服务
/// 提供各个提供商的定价信息
/// </summary>
public static class LLMPricingService
{
    private static readonly Dictionary<string, LLMPricingConfig> _pricingConfigs = new()
    {
        // OpenAI GPT-4o
        ["OpenAI:gpt-4o"] = new LLMPricingConfig
        {
            Provider = "OpenAI",
            Model = "gpt-4o",
            PromptPricePerMillionTokens = 5.00m,        // $5/1M tokens
            CompletionPricePerMillionTokens = 15.00m    // $15/1M tokens
        },
        
        // OpenAI GPT-4o-mini
        ["OpenAI:gpt-4o-mini"] = new LLMPricingConfig
        {
            Provider = "OpenAI",
            Model = "gpt-4o-mini",
            PromptPricePerMillionTokens = 0.150m,       // $0.15/1M tokens
            CompletionPricePerMillionTokens = 0.600m    // $0.60/1M tokens
        },
        
        // OpenAI GPT-4 Turbo
        ["OpenAI:gpt-4-turbo"] = new LLMPricingConfig
        {
            Provider = "OpenAI",
            Model = "gpt-4-turbo",
            PromptPricePerMillionTokens = 10.00m,       // $10/1M tokens
            CompletionPricePerMillionTokens = 30.00m    // $30/1M tokens
        },
        
        // OpenAI GPT-3.5 Turbo
        ["OpenAI:gpt-3.5-turbo"] = new LLMPricingConfig
        {
            Provider = "OpenAI",
            Model = "gpt-3.5-turbo",
            PromptPricePerMillionTokens = 0.500m,       // $0.50/1M tokens
            CompletionPricePerMillionTokens = 1.500m    // $1.50/1M tokens
        },
        
        // DeepSeek Coder
        ["DeepSeek:deepseek-coder"] = new LLMPricingConfig
        {
            Provider = "DeepSeek",
            Model = "deepseek-coder",
            PromptPricePerMillionTokens = 0.140m,       // $0.14/1M tokens
            CompletionPricePerMillionTokens = 0.280m    // $0.28/1M tokens
        },
        
        // DeepSeek Chat
        ["DeepSeek:deepseek-chat"] = new LLMPricingConfig
        {
            Provider = "DeepSeek",
            Model = "deepseek-chat",
            PromptPricePerMillionTokens = 0.140m,       // $0.14/1M tokens
            CompletionPricePerMillionTokens = 0.280m    // $0.28/1M tokens
        },
        
        // Azure OpenAI (价格与OpenAI相同)
        ["Azure:gpt-4o"] = new LLMPricingConfig
        {
            Provider = "Azure",
            Model = "gpt-4o",
            PromptPricePerMillionTokens = 5.00m,
            CompletionPricePerMillionTokens = 15.00m
        },
        
        ["Azure:gpt-4o-mini"] = new LLMPricingConfig
        {
            Provider = "Azure",
            Model = "gpt-4o-mini",
            PromptPricePerMillionTokens = 0.150m,
            CompletionPricePerMillionTokens = 0.600m
        }
    };
    
    /// <summary>
    /// 获取定价配置
    /// </summary>
    public static LLMPricingConfig? GetPricing(string provider, string model)
    {
        var key = $"{provider}:{model}";
        return _pricingConfigs.TryGetValue(key, out var config) ? config : null;
    }
    
    /// <summary>
    /// 计算成本
    /// </summary>
    public static (decimal promptCost, decimal completionCost, decimal totalCost) CalculateCost(
        string provider, 
        string model, 
        int promptTokens, 
        int completionTokens)
    {
        var pricing = GetPricing(provider, model);
        if (pricing == null)
        {
            return (0m, 0m, 0m);
        }
        
        var promptCost = (promptTokens / 1_000_000m) * pricing.PromptPricePerMillionTokens;
        var completionCost = (completionTokens / 1_000_000m) * pricing.CompletionPricePerMillionTokens;
        var totalCost = promptCost + completionCost;
        
        return (promptCost, completionCost, totalCost);
    }
    
    /// <summary>
    /// 获取所有定价配置
    /// </summary>
    public static IEnumerable<LLMPricingConfig> GetAllPricing()
    {
        return _pricingConfigs.Values;
    }
    
    /// <summary>
    /// 添加或更新定价配置
    /// </summary>
    public static void AddOrUpdatePricing(LLMPricingConfig config)
    {
        var key = $"{config.Provider}:{config.Model}";
        _pricingConfigs[key] = config;
    }
}
