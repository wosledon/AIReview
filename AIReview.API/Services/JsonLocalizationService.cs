using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Text.Json;

namespace AIReview.API.Services;

/// <summary>
/// JSON-based 本地化服务 (类似ABP框架)
/// </summary>
public class JsonLocalizationService : IStringLocalizer
{
    private readonly ILogger<JsonLocalizationService> _logger;
    private readonly string _resourcesPath;
    private readonly CultureInfo _culture;
    private Dictionary<string, string>? _localizedStrings;

    public JsonLocalizationService(
        ILogger<JsonLocalizationService> logger,
        string resourcesPath,
        CultureInfo culture)
    {
        _logger = logger;
        _resourcesPath = resourcesPath;
        _culture = culture;
    }

    private Dictionary<string, string> GetLocalizedStrings()
    {
        if (_localizedStrings != null)
            return _localizedStrings;

        _localizedStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // 尝试加载当前文化的JSON文件
            var fileName = $"{_culture.Name}.json";
            var filePath = Path.Combine(_resourcesPath, fileName);

            // 如果当前文化文件不存在，尝试加载默认文化 (en-US.json 或默认文件)
            if (!File.Exists(filePath))
            {
                fileName = "zh-CN.json"; // 默认使用中文
                filePath = Path.Combine(_resourcesPath, fileName);
            }

            if (File.Exists(filePath))
            {
                var jsonContent = File.ReadAllText(filePath);
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (deserialized != null)
                {
                    _localizedStrings = new Dictionary<string, string>(deserialized, StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localization file for culture {Culture}", _culture.Name);
        }

        return _localizedStrings;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var strings = GetLocalizedStrings();
            if (strings.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, value, false);
            }

            // 如果找不到本地化字符串，返回key本身
            _logger.LogWarning("Localization key '{Key}' not found for culture {Culture}", name, _culture.Name);
            return new LocalizedString(name, name, true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var strings = GetLocalizedStrings();
            if (strings.TryGetValue(name, out var value))
            {
                try
                {
                    var formatted = string.Format(value, arguments);
                    return new LocalizedString(name, formatted, false);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Failed to format localized string '{Key}' with arguments", name);
                    return new LocalizedString(name, value, false);
                }
            }

            return new LocalizedString(name, name, true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var strings = GetLocalizedStrings();
        return strings.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, false));
    }
}

/// <summary>
/// JSON本地化工厂
/// </summary>
public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _resourcesPath;

    public JsonStringLocalizerFactory(IHostEnvironment environment, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _resourcesPath = Path.Combine(environment.ContentRootPath, "Resources");
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var culture = CultureInfo.CurrentUICulture;
        var logger = _loggerFactory.CreateLogger<JsonLocalizationService>();
        return new JsonLocalizationService(logger, _resourcesPath, culture);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        var culture = CultureInfo.CurrentUICulture;
        var logger = _loggerFactory.CreateLogger<JsonLocalizationService>();
        return new JsonLocalizationService(logger, _resourcesPath, culture);
    }
}