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
using Prometheus;
using AIReview.Core.Entities;
using AIReview.Core.Interfaces;
using AIReview.Core.Services;
using AIReview.Infrastructure;
using AIReview.Infrastructure.Data;
using AIReview.Infrastructure.Services;
using AIReview.Infrastructure.BackgroundJobs;
using AIReview.API.Hubs;
using AIReview.API.Services;

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
builder.Services.AddControllers();
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
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 配置Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// 注册应用服务
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// 注册AI服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMConfigurationService, LLMConfigurationService>();
builder.Services.AddScoped<IMultiLLMService, MultiLLMService>();
builder.Services.AddScoped<ILLMService, OpenAIService>();
builder.Services.AddScoped<IContextBuilder, ContextBuilder>();
builder.Services.AddScoped<IAIReviewer, AIReviewer>();

// 注册后台任务服务
builder.Services.AddScoped<IAIReviewService, HangfireAIReviewService>();
builder.Services.AddScoped<AIReviewJob>();

// 注册通知服务
builder.Services.AddScoped<AIReview.Core.Interfaces.INotificationService, NotificationService>();

// 配置SignalR
builder.Services.AddSignalR();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 添加健康检查（暂不使用 AddDbContextCheck 以避免依赖缺失）
builder.Services.AddHealthChecks();

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

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// 添加Prometheus监控中间件
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapMetrics(); // Prometheus指标端点
app.MapHealthChecks("/health");

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.Migrate();
        Log.Information("Database migration completed");
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