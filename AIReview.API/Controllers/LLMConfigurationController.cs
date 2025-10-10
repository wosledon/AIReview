using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;

namespace AIReview.API.Controllers;

/// <summary>
/// LLM配置管理控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class LLMConfigurationController : ControllerBase
{
    private readonly ILLMConfigurationService _configurationService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IMultiLLMService _multiLLMService;

    public LLMConfigurationController(
        ILLMConfigurationService configurationService,
        ILLMProviderFactory providerFactory,
        IMultiLLMService multiLLMService)
    {
        _configurationService = configurationService;
        _providerFactory = providerFactory;
        _multiLLMService = multiLLMService;
    }

    /// <summary>
    /// 获取所有活动的LLM配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LLMConfigurationDto>>> GetConfigurations()
    {
        var configurations = await _configurationService.GetAllActiveAsync();
        var result = configurations.Select(c => new LLMConfigurationDto
        {
            Id = c.Id,
            Name = c.Name,
            Provider = c.Provider,
            ApiEndpoint = c.ApiEndpoint,
            Model = c.Model,
            MaxTokens = c.MaxTokens,
            Temperature = c.Temperature,
            IsActive = c.IsActive,
            IsDefault = c.IsDefault,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });
        
        return Ok(result);
    }

    /// <summary>
    /// 获取指定ID的LLM配置
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LLMConfigurationDto>> GetConfiguration(int id)
    {
        var configuration = await _configurationService.GetByIdAsync(id);
        if (configuration == null)
        {
            return NotFound($"找不到ID为 {id} 的LLM配置");
        }

        var result = new LLMConfigurationDto
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Provider = configuration.Provider,
            ApiEndpoint = configuration.ApiEndpoint,
            Model = configuration.Model,
            MaxTokens = configuration.MaxTokens,
            Temperature = configuration.Temperature,
            IsActive = configuration.IsActive,
            IsDefault = configuration.IsDefault,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };

        return Ok(result);
    }

    /// <summary>
    /// 创建新的LLM配置
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LLMConfigurationDto>> CreateConfiguration(CreateLLMConfigurationDto dto)
    {
        var configuration = new LLMConfiguration
        {
            Name = dto.Name,
            Provider = dto.Provider,
            ApiEndpoint = dto.ApiEndpoint,
            ApiKey = dto.ApiKey,
            Model = dto.Model,
            MaxTokens = dto.MaxTokens,
            Temperature = dto.Temperature,
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            ExtraParameters = dto.ExtraParameters
        };

        try
        {
            var created = await _configurationService.CreateAsync(configuration);
            
            var result = new LLMConfigurationDto
            {
                Id = created.Id,
                Name = created.Name,
                Provider = created.Provider,
                ApiEndpoint = created.ApiEndpoint,
                Model = created.Model,
                MaxTokens = created.MaxTokens,
                Temperature = created.Temperature,
                IsActive = created.IsActive,
                IsDefault = created.IsDefault,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            };

            return CreatedAtAction(nameof(GetConfiguration), new { id = created.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新LLM配置
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<LLMConfigurationDto>> UpdateConfiguration(int id, UpdateLLMConfigurationDto dto)
    {
        var configuration = new LLMConfiguration
        {
            Id = id,
            Name = dto.Name,
            Provider = dto.Provider,
            ApiEndpoint = dto.ApiEndpoint,
            ApiKey = dto.ApiKey,
            Model = dto.Model,
            MaxTokens = dto.MaxTokens,
            Temperature = dto.Temperature,
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            ExtraParameters = dto.ExtraParameters
        };

        try
        {
            var updated = await _configurationService.UpdateAsync(configuration);
            
            var result = new LLMConfigurationDto
            {
                Id = updated.Id,
                Name = updated.Name,
                Provider = updated.Provider,
                ApiEndpoint = updated.ApiEndpoint,
                Model = updated.Model,
                MaxTokens = updated.MaxTokens,
                Temperature = updated.Temperature,
                IsActive = updated.IsActive,
                IsDefault = updated.IsDefault,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            };

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 删除LLM配置
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteConfiguration(int id)
    {
        try
        {
            await _configurationService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 设置默认LLM配置
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(int id)
    {
        try
        {
            await _configurationService.SetDefaultAsync(id);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 测试LLM配置连接
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<TestConnectionResultDto>> TestConnection(int id)
    {
        var configuration = await _configurationService.GetByIdAsync(id);
        if (configuration == null)
        {
            return NotFound($"找不到ID为 {id} 的LLM配置");
        }

        var isSuccess = await _configurationService.TestConnectionAsync(configuration);
        
        return Ok(new TestConnectionResultDto
        {
            Success = isSuccess,
            Message = isSuccess ? "连接测试成功" : "连接测试失败，请检查配置"
        });
    }

    /// <summary>
    /// 获取支持的LLM提供商列表
    /// </summary>
    [HttpGet("providers")]
    public ActionResult<IEnumerable<string>> GetSupportedProviders()
    {
        var providers = _providerFactory.GetSupportedProviders();
        return Ok(providers);
    }

    /// <summary>
    /// 获取当前默认配置
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<LLMConfigurationDto>> GetDefaultConfiguration()
    {
        var configuration = await _configurationService.GetDefaultConfigurationAsync();
        if (configuration == null)
        {
            return NotFound("没有设置默认LLM配置");
        }

        var result = new LLMConfigurationDto
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Provider = configuration.Provider,
            ApiEndpoint = configuration.ApiEndpoint,
            Model = configuration.Model,
            MaxTokens = configuration.MaxTokens,
            Temperature = configuration.Temperature,
            IsActive = configuration.IsActive,
            IsDefault = configuration.IsDefault,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };

        return Ok(result);
    }
}

// DTOs
public class LLMConfigurationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLLMConfigurationDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.3;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public string? ExtraParameters { get; set; }
}

public class UpdateLLMConfigurationDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.3;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public string? ExtraParameters { get; set; }
}

public class TestConnectionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}