# 多语言支持 (JSON-based, 类似ABP框架)

本项目实现了基于JSON的多语言支持，类似于ABP框架的方式。

## 支持的语言

- **en-US**: English (US)
- **zh-CN**: 中文(简体) - 默认语言

## 文件结构

```
Resources/
├── zh-CN.json    # 中文资源 (默认)
├── en-US.json    # 英文资源
└── README.md     # 使用说明
```

## JSON格式

资源文件使用简单的key-value JSON格式：

```json
{
  "NotFound": "Resource not found",
  "Forbidden": "Access forbidden",
  "ProjectNotFound": "Project not found"
}
```

## API使用方法

### 1. 设置请求语言

通过以下方式之一设置语言：

- **Accept-Language header**: `Accept-Language: zh-CN`
- **Query parameter**: `?culture=zh-CN`
- **Cookie**: `.AspNetCore.Culture=c=zh-CN|uic=zh-CN`

### 2. 测试端点

#### 获取本地化消息
```
GET /api/localization/message?key=OperationSuccessful
```

#### 测试异常本地化
```
GET /api/localization/error?errorKey=NotFound
```

#### 获取支持的文化
```
GET /api/localization/cultures
```

## 在代码中使用

### 注入本地化服务

```csharp
public class MyController : ControllerBase
{
    private readonly IStringLocalizer _localizer;

    public MyController(IStringLocalizerFactory localizerFactory)
    {
        _localizer = localizerFactory.Create(typeof(SharedResource));
    }

    public IActionResult MyAction()
    {
        var message = _localizer["OperationSuccessful"];
        return Ok(message.Value);
    }
}
```

### 使用本地化异常服务

```csharp
public class MyService
{
    private readonly ILocalizedExceptionService _exceptionService;

    public MyService(ILocalizedExceptionService exceptionService)
    {
        _exceptionService = exceptionService;
    }

    public void DoSomething()
    {
        if (someCondition)
        {
            throw _exceptionService.CreateLocalizedException("NotFound");
        }
    }
}
```

## 添加新的本地化资源

1. 在所有JSON文件中添加新的key-value对
2. 在代码中使用 `_localizer["YourNewKey"]` 访问

## 扩展支持更多语言

1. 创建新的JSON文件：`{culture}.json` (例如: `fr-FR.json`, `ja-JP.json`)
2. 在 `Program.cs` 的 `supportedCultures` 中添加新文化
3. 更新默认文化（如果需要）

## 特性

- ✅ JSON格式，易于编辑和版本控制
- ✅ 类似ABP框架的实现方式
- ✅ 支持参数化消息
- ✅ 自动fallback到默认语言
- ✅ 全局异常处理器自动本地化错误消息
- ✅ 支持请求头、查询参数和Cookie设置语言