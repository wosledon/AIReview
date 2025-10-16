using Microsoft.Extensions.Logging;
using AIReview.Core.Interfaces;
using System.Text.RegularExpressions;

namespace AIReview.Infrastructure.Services;

public class ContextBuilder : IContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;

    // 项目类型检测规则
    private static readonly Dictionary<string, List<ProjectTypeRule>> ProjectTypeRules = new()
    {
        ["csharp"] = new()
        {
            new("ASP.NET Core Web API", new[] { "Controller", "ApiController", "[HttpGet]", "[HttpPost]", "IActionResult" }),
            new("ASP.NET Core MVC", new[] { "Controller", "View", "Model", "ViewResult" }),
            new("Blazor", new[] { "@page", "@code", "Blazor", "ComponentBase" }),
            new("Entity Framework", new[] { "DbContext", "DbSet", "Entity", "Migration" }),
            new("WPF Application", new[] { "Window", "UserControl", "DependencyProperty" }),
            new("Xamarin", new[] { "Xamarin", "ContentPage", "BindableProperty" }),
            new("Console Application", new[] { "Main(", "Console.WriteLine" }),
            new("Class Library", new[] { "namespace", "class", "public" }),
            new("Unit Test", new[] { "[Test]", "[Fact]", "Assert", "xunit", "NUnit", "MSTest" })
        },
        ["javascript"] = new()
        {
            new("React Application", new[] { "React", "Component", "useState", "useEffect", "jsx", "tsx" }),
            new("Vue.js Application", new[] { "Vue", "vue", "template", "v-bind", "v-for" }),
            new("Angular Application", new[] { "@Component", "@NgModule", "angular", "ng-" }),
            new("Node.js Express", new[] { "express", "app.listen", "router", "middleware" }),
            new("Next.js Application", new[] { "next", "getStaticProps", "getServerSideProps" }),
            new("TypeScript Application", new[] { "interface", "type", ": string", ": number" }),
            new("Webpack Configuration", new[] { "webpack", "module.exports", "entry:", "output:" })
        },
        ["python"] = new()
        {
            new("Django Application", new[] { "django", "models.Model", "views", "urls.py" }),
            new("Flask Application", new[] { "flask", "Flask(__name__)", "@app.route" }),
            new("FastAPI Application", new[] { "fastapi", "FastAPI()", "@app.get" }),
            new("Data Science", new[] { "pandas", "numpy", "matplotlib", "sklearn", "tensorflow" }),
            new("Machine Learning", new[] { "keras", "pytorch", "tensorflow", "model.fit" }),
            new("Unit Test", new[] { "unittest", "pytest", "test_", "TestCase" })
        }
    };

    // 编码规范映射
    private static readonly Dictionary<string, CodingStandard> CodingStandards = new()
    {
        ["csharp"] = new("Microsoft C# Coding Conventions", 
            "- 使用PascalCase命名类和方法\n- 使用camelCase命名私有字段\n- 接口以I开头\n- 使用有意义的名称"),
        ["javascript"] = new("Airbnb JavaScript Style Guide", 
            "- 使用const和let替代var\n- 使用箭头函数\n- 使用模板字符串\n- 避免全局变量"),
        ["typescript"] = new("TypeScript Style Guide", 
            "- 使用接口定义类型\n- 启用strict模式\n- 避免使用any\n- 使用类型推断"),
        ["python"] = new("PEP 8 Style Guide", 
            "- 使用snake_case命名变量和函数\n- 类名使用PascalCase\n- 每行不超过79字符\n- 使用4空格缩进"),
        ["java"] = new("Google Java Style Guide", 
            "- 使用驼峰命名法\n- 类名首字母大写\n- 常量全大写\n- 每行不超过100字符"),
        ["go"] = new("Effective Go", 
            "- 使用gofmt格式化代码\n- 简洁命名\n- 错误处理不使用异常\n- 使用接口")
    };

    public ContextBuilder(ILogger<ContextBuilder> logger)
    {
        _logger = logger;
    }

    public Task<ReviewContext> BuildContextAsync(string diff, ReviewContext baseContext)
    {
        try
        {
            var language = NormalizeLanguage(baseContext.Language);
            
            // 分析代码差异以提取更多上下文信息
            var context = new ReviewContext
            {
                Language = language,
                ProjectType = DetermineProjectType(diff, language),
                CodingStandards = GetCodingStandards(language)
            };

            _logger.LogDebug("Context built: Language={Language}, ProjectType={ProjectType}", 
                context.Language, context.ProjectType);

            return Task.FromResult(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building review context");
            
            // 返回默认上下文而不是抛出异常
            return Task.FromResult(new ReviewContext
            {
                Language = baseContext.Language,
                ProjectType = "General Application",
                CodingStandards = "General Best Practices"
            });
        }
    }

    /// <summary>
    /// 标准化语言名称
    /// </summary>
    private string NormalizeLanguage(string language)
    {
        var languageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["c#"] = "csharp",
            ["cs"] = "csharp",
            ["js"] = "javascript",
            ["ts"] = "typescript",
            ["py"] = "python",
            ["rb"] = "ruby",
            ["go"] = "go",
            ["java"] = "java",
            ["cpp"] = "cpp",
            ["c++"] = "cpp"
        };

        return languageMap.TryGetValue(language, out var normalized) 
            ? normalized 
            : language.ToLowerInvariant();
    }

    /// <summary>
    /// 根据代码差异和语言智能判断项目类型
    /// </summary>
    private string DetermineProjectType(string diff, string language)
    {
        if (!ProjectTypeRules.TryGetValue(language, out var rules))
        {
            _logger.LogDebug("No project type rules found for language: {Language}", language);
            return "General Application";
        }

        // 计算每种项目类型的匹配分数
        var scores = new Dictionary<string, int>();
        
        foreach (var rule in rules)
        {
            var score = CalculateMatchScore(diff, rule.Keywords);
            if (score > 0)
            {
                scores[rule.TypeName] = score;
            }
        }

        if (scores.Count == 0)
        {
            _logger.LogDebug("No project type matched for language: {Language}", language);
            return GetDefaultProjectType(language);
        }

        // 返回得分最高的项目类型
        var bestMatch = scores.OrderByDescending(kv => kv.Value).First();
        
        _logger.LogDebug("Project type detected: {ProjectType} (score: {Score})", 
            bestMatch.Key, bestMatch.Value);
        
        return bestMatch.Key;
    }

    /// <summary>
    /// 计算关键词匹配分数
    /// </summary>
    private int CalculateMatchScore(string diff, string[] keywords)
    {
        var score = 0;
        var lowerDiff = diff.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            var lowerKeyword = keyword.ToLowerInvariant();
            
            // 计算关键词出现次数
            var count = Regex.Matches(lowerDiff, Regex.Escape(lowerKeyword)).Count;
            
            // 权重：更长的关键词权重更高
            var weight = Math.Max(1, keyword.Length / 5);
            
            score += count * weight;
        }

        return score;
    }

    /// <summary>
    /// 获取默认项目类型
    /// </summary>
    private string GetDefaultProjectType(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "csharp" => "Console Application",
            "javascript" or "typescript" => "JavaScript Application",
            "python" => "Python Application",
            "java" => "Java Application",
            "go" => "Go Application",
            _ => "General Application"
        };
    }

    /// <summary>
    /// 获取语言对应的编码规范
    /// </summary>
    private string GetCodingStandards(string language)
    {
        if (CodingStandards.TryGetValue(language, out var standard))
        {
            return $"{standard.Name}\n{standard.Description}";
        }

        return "General Best Practices\n- 保持代码简洁清晰\n- 使用有意义的命名\n- 添加适当的注释\n- 遵循DRY原则";
    }

    /// <summary>
    /// 项目类型规则
    /// </summary>
    private record ProjectTypeRule(string TypeName, string[] Keywords);

    /// <summary>
    /// 编码规范
    /// </summary>
    private record CodingStandard(string Name, string Description);
}