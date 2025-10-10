using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;

namespace AIReview.Infrastructure.Services;

public class OpenAIService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _apiKey = configuration["OpenAI:ApiKey"] ?? ""; // 可为空以便本地开发
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _maxTokens = int.Parse(configuration["OpenAI:MaxTokens"] ?? "4000");
        _temperature = double.Parse(configuration["OpenAI:Temperature"] ?? "0.3");
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    // 构造函数重载，支持直接传入配置参数
    public OpenAIService(
        HttpClient httpClient,
        ILogger<OpenAIService> logger,
        string apiKey,
        string model = "gpt-4o-mini",
        int maxTokens = 4000,
        double temperature = 0.3,
        string apiEndpoint = "https://api.openai.com/v1/")
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        _temperature = temperature;
        
        _httpClient.BaseAddress = new Uri(apiEndpoint.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        try
        {
            // 轻量占位实现：如果没有配置API Key，则返回一个本地提示，避免阻塞开发
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured. Returning mock response.");
                return "[Mock AI Review] 没有配置OpenAI API Key，返回示例评审：请注意代码的异常处理、单元测试覆盖率和日志记录。";
            }

            var request = new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "system", content = "You are an expert code reviewer. Reply in Chinese." },
                    new { role = "user", content = prompt }
                },
                temperature = _temperature,
                max_tokens = _maxTokens
            };

            using var response = await _httpClient.PostAsJsonAsync("chat/completions", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<dynamic>();
            var content = (string?)json?.choices?[0]?.message?.content;

            if (!string.IsNullOrWhiteSpace(content))
            {
                return content!;
            }

            throw new InvalidOperationException("No content returned from OpenAI API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return false;
            }

            var request = new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "user", content = "Hello" }
                },
                max_tokens = 10,
                temperature = 0.1
            };

            using var response = await _httpClient.PostAsJsonAsync("chat/completions", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing OpenAI connection");
            return false;
        }
    }
}