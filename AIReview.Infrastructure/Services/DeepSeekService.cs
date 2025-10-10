using AIReview.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AIReview.Infrastructure.Services;

/// <summary>
/// DeepSeek LLM服务实现
/// </summary>
public class DeepSeekService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly string _apiEndpoint;

    public DeepSeekService(
        HttpClient httpClient,
        ILogger<DeepSeekService> logger,
        string apiKey,
        string model = "deepseek-coder",
        int maxTokens = 4000,
        double temperature = 0.3,
        string apiEndpoint = "https://api.deepseek.com/v1")
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        _temperature = temperature;
        _apiEndpoint = apiEndpoint.TrimEnd('/');
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _maxTokens,
                temperature = _temperature,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiEndpoint}/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API请求失败: {StatusCode}, {Content}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"DeepSeek API请求失败: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            
            var choices = responseJson.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                var result = message.GetProperty("content").GetString();
                return result ?? "未能生成审查结果";
            }

            return "未能生成审查结果";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用DeepSeek API时发生错误");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                max_tokens = 10,
                temperature = 0.1
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiEndpoint}/chat/completions", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试DeepSeek连接时发生错误");
            return false;
        }
    }
}