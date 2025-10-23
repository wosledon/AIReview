using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;
using Hangfire;
using Hangfire.SQLite;
using Hangfire.Redis.StackExchange;
using Prometheus;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Core.Services;
using AIReview.Infrastructure;
using AIReview.Infrastructure.Data;
using AIReview.Infrastructure.Services;
using AIReview.Infrastructure.Repositories;
using AIReview.Infrastructure.BackgroundJobs;
using AIReview.API.Hubs;
using AIReview.API.Services;
using AIReview.API.Middleware;
using Microsoft.Extensions.Options;
using AIReview.Shared.Enums;

var builder = WebApplication.CreateBuilder(args);

// // 配置Serilog
// Log.Logger = new LoggerConfiguration()
//     .MinimumLevel.Information()
//     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
//     .MinimumLevel.Override("System", LogEventLevel.Warning)
//     .Enrich.FromLogContext()
//     .WriteTo.Console()
//     .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
//     .CreateLogger();

// builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // 允许使用枚举名称进行序列化/反序列化（例如 "Info"、"Warning"）
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();

// 配置Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "AI Review API", 
        Version = "v1",
        Description = "AI代码评审平台API"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 配置Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 配置JWT认证
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["Secret"] ?? throw new ArgumentException("JWT Secret is required");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    
    // 为SignalR配置JWT认证
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 配置授权
builder.Services.AddAuthorization();

// 配置缓存
builder.Services.AddMemoryCache();

// 尝试配置 Redis 缓存，失败则仅使用内存缓存
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var redisAvailableForCache = false;
StackExchange.Redis.IConnectionMultiplexer? redisConnection = null;

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        // 配置Redis连接
        var testConfig = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        testConfig.AbortOnConnectFail = false;
        testConfig.ConnectTimeout = 5000;
        testConfig.SyncTimeout = 5000;
        testConfig.ConnectRetry = 3;
        
        redisConnection = StackExchange.Redis.ConnectionMultiplexer.Connect(testConfig);
        
        if (redisConnection.IsConnected)
        {
            redisAvailableForCache = true;
            
            // 注册 IConnectionMultiplexer (单例)
            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                
                // 连接事件日志
                redisConnection.ConnectionRestored += (sender, args) =>
                    logger.LogInformation("Redis连接已恢复: {EndPoint}", args.EndPoint);
                redisConnection.ConnectionFailed += (sender, args) =>
                    logger.LogError("Redis连接失败: {EndPoint}, {FailureType}", args.EndPoint, args.FailureType);
                redisConnection.ErrorMessage += (sender, args) =>
                    logger.LogError("Redis错误: {Message}", args.Message);
                
                logger.LogInformation("Redis连接已建立: {Endpoints}", 
                    string.Join(", ", redisConnection.GetEndPoints().Select(e => e.ToString())));
                
                return redisConnection;
            });
            
            // 配置分布式缓存
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = testConfig;
                options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "AIReview:";
            });
            
            // 注册分布式缓存服务
            builder.Services.AddScoped<IDistributedCacheService, RedisDistributedCacheService>();
            
            // 注册Job幂等性服务
            builder.Services.AddScoped<IJobIdempotencyService, JobIdempotencyService>();
            
            Console.WriteLine("✓ Redis distributed cache and idempotency services configured successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Redis cache unavailable, using memory cache only: {ex.Message}");
        redisConnection?.Dispose();
        redisConnection = null;
    }
}

if (!redisAvailableForCache)
{
    Console.WriteLine("ℹ Using in-memory cache (Redis not available)");
    Console.WriteLine("⚠ Falling back to in-memory implementations for distributed services");
    Console.WriteLine("⚠ In-memory services only work in single-instance deployments");
    
    // 提供 IDistributedCache 的内存实现（用于 InMemoryDistributedCacheService 依赖）
    builder.Services.AddDistributedMemoryCache();
    
    // 注册内存版本的分布式缓存服务
    builder.Services.AddScoped<IDistributedCacheService, InMemoryDistributedCacheService>();
    
    // 注册内存版本的Job幂等性服务
    builder.Services.AddScoped<IJobIdempotencyService, InMemoryJobIdempotencyService>();
}

// 配置Hangfire（优先使用 Redis 实现分布式；Redis 不可用时降级到 SQLite）
var useRedisForHangfire = false;
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        // 测试 Redis 连接（快速失败，避免阻塞启动）
        var redisConfig = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        redisConfig.AbortOnConnectFail = false;
        redisConfig.ConnectTimeout = 2000;
        redisConfig.SyncTimeout = 2000;
        redisConfig.ConnectRetry = 1;
        
        using var testConnection = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig);
        if (testConnection.IsConnected)
        {
            useRedisForHangfire = true;
            Console.WriteLine("✓ Redis available for Hangfire distributed storage");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Redis unavailable for Hangfire, falling back to SQLite: {ex.Message}");
    }
}

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    if (useRedisForHangfire)
    {
        var redisConfig = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString!);
        redisConfig.AbortOnConnectFail = false;
        redisConfig.ConnectTimeout = 5000;
        redisConfig.SyncTimeout = 5000;
        
        configuration.UseRedisStorage(redisConfig.ToString(), new RedisStorageOptions
        {
            Db = 5,
            Prefix = "hangfire:aireview:"
        });
        Console.WriteLine("✓ Hangfire configured with Redis storage (distributed mode)");
    }
    else
    {
        // 降级：使用 SQLite 本地存储
        var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection");
        configuration.UseSQLiteStorage(sqliteConnection);
        Console.WriteLine("✓ Hangfire configured with SQLite storage (single-instance mode)");
    }
});

// 启动 Hangfire Server 并监听 ai-review 和 ai-analysis 队列
// 配置合理的 Worker 数量，避免过度并发导致重复任务
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "ai-review", "ai-analysis" };
    // 限制 Worker 数量为 2，配合 DisableConcurrentExecution 特性避免重复执行
    // AI 分析任务通常是 I/O 密集型，不需要太多并发
    options.WorkerCount = 2;
    
    // 设置合理的轮询间隔，减少数据库压力
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// 注册应用服务
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDiffParserService, DiffParserService>();
builder.Services.AddScoped<ProjectGitMigrationService>();

// 注册分析服务
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
builder.Services.AddScoped<IImprovementSuggestionService, ImprovementSuggestionService>();
builder.Services.AddScoped<IPullRequestAnalysisService, PullRequestAnalysisService>();

// 注册Git服务
builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<IGitRepositoryStatusService, GitRepositoryStatusService>();
builder.Services.AddScoped<IGitCredentialService, GitCredentialService>();

// 注册AI服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMConfigurationService, LLMConfigurationService>();
builder.Services.AddScoped<IMultiLLMService, MultiLLMService>();
builder.Services.AddScoped<IContextBuilder, ContextBuilder>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ChunkedReviewService>(); // 分块评审服务
builder.Services.AddScoped<IAIReviewer, AIReviewer>();
builder.Services.AddScoped<IPromptService, PromptService>();

// 绑定分块评审/分析的选项（可在 appsettings.json 的 ChunkedReview 节点配置）
builder.Services.Configure<ChunkedReviewOptions>(builder.Configuration.GetSection("ChunkedReview"));

// 注册后台任务服务
builder.Services.AddScoped<IAIReviewService, HangfireAIReviewService>();
builder.Services.AddScoped<IAsyncAnalysisService, HangfireAsyncAnalysisService>();
builder.Services.AddScoped<AIReviewJob>();
builder.Services.AddScoped<AIAnalysisJob>();

// 注册通知服务
builder.Services.AddScoped<AIReview.Core.Interfaces.INotificationService, NotificationService>();

// 注册 Token 使用跟踪服务
builder.Services.AddScoped<ITokenUsageRepository, TokenUsageRepository>();
builder.Services.AddScoped<ITokenUsageService, AIReview.Core.Services.TokenUsageService>();

// 配置SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://10.60.33.81:5173", "http://127.0.0.1:5173", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// 添加健康检查（暂不使用 AddDbContextCheck 以避免依赖缺失）
builder.Services.AddHealthChecks();

builder.WebHost.UseUrls("http://*:5000");

var app = builder.Build();

// 配置中间件管道
if (app.Environment.IsDevelopment())
{

}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Review API v1");
    c.RoutePrefix = string.Empty; // 让Swagger UI在根路径可访问
});

app.UseHangfireDashboard("/hangfire");

// CORS必须在其他中间件之前
app.UseCors("AllowFrontend");

// app.UseHttpsRedirection();

// 添加Prometheus监控中间件
app.UseHttpMetrics();

// 全局异常处理中间件（必须在其他中间件之前）
app.UseGlobalExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");
app.MapMetrics(); // Prometheus指标端点
app.MapHealthChecks("/health");

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        context.Database.Migrate();
        Log.Information("Database migration completed");

        // Seed default built-in prompt templates if none exist for all four types
        // Insert only when the table has no entries for these types to avoid duplicates
        var needSeed = !context.PromptConfigurations.Any(pc => pc.Type == PromptType.Review)
                        || !context.PromptConfigurations.Any(pc => pc.Type == PromptType.RiskAnalysis)
                        || !context.PromptConfigurations.Any(pc => pc.Type == PromptType.PullRequestSummary)
                        || !context.PromptConfigurations.Any(pc => pc.Type == PromptType.ImprovementSuggestions);
        if (needSeed)
        {
            var now = DateTime.UtcNow;
            if (!context.PromptConfigurations.Any(pc => pc.Type == PromptType.Review))
            {
                context.PromptConfigurations.Add(new AIReview.Core.Entities.PromptConfiguration
                {
                    Type = PromptType.Review,
                    Name = "内置-代码评审模板",
                    Content = AIReview.Infrastructure.Services.PromptService.GetBuiltInTemplate(PromptType.Review),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            if (!context.PromptConfigurations.Any(pc => pc.Type == PromptType.RiskAnalysis))
            {
                context.PromptConfigurations.Add(new AIReview.Core.Entities.PromptConfiguration
                {
                    Type = PromptType.RiskAnalysis,
                    Name = "内置-风险分析模板",
                    Content = AIReview.Infrastructure.Services.PromptService.GetBuiltInTemplate(PromptType.RiskAnalysis),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            if (!context.PromptConfigurations.Any(pc => pc.Type == PromptType.PullRequestSummary))
            {
                context.PromptConfigurations.Add(new AIReview.Core.Entities.PromptConfiguration
                {
                    Type = PromptType.PullRequestSummary,
                    Name = "内置-变更摘要模板",
                    Content = AIReview.Infrastructure.Services.PromptService.GetBuiltInTemplate(PromptType.PullRequestSummary),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            if (!context.PromptConfigurations.Any(pc => pc.Type == PromptType.ImprovementSuggestions))
            {
                context.PromptConfigurations.Add(new AIReview.Core.Entities.PromptConfiguration
                {
                    Type = PromptType.ImprovementSuggestions,
                    Name = "内置-改进建议模板",
                    Content = AIReview.Infrastructure.Services.PromptService.GetBuiltInTemplate(PromptType.ImprovementSuggestions),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default Prompt templates (built-in) if missing");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database migration");
    }
}

try
{
    Log.Information("Starting AI Review API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}