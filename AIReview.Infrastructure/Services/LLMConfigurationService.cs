using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// LLM配置管理服务实现
/// </summary>
public class LLMConfigurationService : ILLMConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<LLMConfigurationService> _logger;

    public LLMConfigurationService(
        ApplicationDbContext context,
        ILLMProviderFactory providerFactory,
        ILogger<LLMConfigurationService> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<LLMConfiguration?> GetDefaultConfigurationAsync()
    {
        return await _context.LLMConfigurations
            .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive);
    }

    public async Task<LLMConfiguration?> GetByIdAsync(int id)
    {
        return await _context.LLMConfigurations
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<LLMConfiguration>> GetAllActiveAsync()
    {
        return await _context.LLMConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<LLMConfiguration> CreateAsync(LLMConfiguration configuration)
    {
        // 验证提供商是否支持
        if (!_providerFactory.IsProviderSupported(configuration.Provider))
        {
            throw new ArgumentException($"不支持的LLM提供商: {configuration.Provider}");
        }

        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;

        // 如果设置为默认，取消其他默认配置
        if (configuration.IsDefault)
        {
            await ClearDefaultAsync();
        }

        _context.LLMConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("创建LLM配置 {Name} (Provider: {Provider})", 
            configuration.Name, configuration.Provider);

        return configuration;
    }

    public async Task<LLMConfiguration> UpdateAsync(LLMConfiguration configuration)
    {
        var existing = await GetByIdAsync(configuration.Id);
        if (existing == null)
        {
            throw new ArgumentException($"找不到ID为 {configuration.Id} 的LLM配置");
        }

        // 验证提供商是否支持
        if (!_providerFactory.IsProviderSupported(configuration.Provider))
        {
            throw new ArgumentException($"不支持的LLM提供商: {configuration.Provider}");
        }

        // 如果设置为默认，取消其他默认配置
        if (configuration.IsDefault && !existing.IsDefault)
        {
            await ClearDefaultAsync();
        }

        existing.Name = configuration.Name;
        existing.Provider = configuration.Provider;
        existing.ApiEndpoint = configuration.ApiEndpoint;
        existing.ApiKey = configuration.ApiKey;
        existing.Model = configuration.Model;
        existing.MaxTokens = configuration.MaxTokens;
        existing.Temperature = configuration.Temperature;
        existing.IsActive = configuration.IsActive;
        existing.IsDefault = configuration.IsDefault;
        existing.ExtraParameters = configuration.ExtraParameters;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("更新LLM配置 {Name} (Provider: {Provider})", 
            existing.Name, existing.Provider);

        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var configuration = await GetByIdAsync(id);
        if (configuration == null)
        {
            throw new ArgumentException($"找不到ID为 {id} 的LLM配置");
        }

        if (configuration.IsDefault)
        {
            throw new InvalidOperationException("不能删除默认LLM配置，请先设置其他配置为默认");
        }

        _context.LLMConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("删除LLM配置 {Name} (ID: {Id})", configuration.Name, id);
    }

    public async Task SetDefaultAsync(int id)
    {
        var configuration = await GetByIdAsync(id);
        if (configuration == null)
        {
            throw new ArgumentException($"找不到ID为 {id} 的LLM配置");
        }

        if (!configuration.IsActive)
        {
            throw new InvalidOperationException("不能将非活动的配置设置为默认");
        }

        // 取消其他默认配置
        await ClearDefaultAsync();

        configuration.IsDefault = true;
        configuration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("设置LLM配置 {Name} 为默认", configuration.Name);
    }

    public async Task<bool> TestConnectionAsync(LLMConfiguration configuration)
    {
        try
        {
            var provider = _providerFactory.CreateProvider(configuration);
            return await provider.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试LLM配置连接失败 {Name} (Provider: {Provider})", 
                configuration.Name, configuration.Provider);
            return false;
        }
    }

    private async Task ClearDefaultAsync()
    {
        var defaultConfigurations = await _context.LLMConfigurations
            .Where(c => c.IsDefault)
            .ToListAsync();

        foreach (var config in defaultConfigurations)
        {
            config.IsDefault = false;
            config.UpdatedAt = DateTime.UtcNow;
        }

        if (defaultConfigurations.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}