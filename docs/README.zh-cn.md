AIReview — AI 驱动的代码评审平台
=================================

**此项目完全由 Copilot 生成; 0人工, 100% AI.**

简体中文 | [English](../README.md)

## 🎯 项目概述

AIReview 是一款企业级 AI 驱动的代码评审平台,通过智能自动化变革代码评审流程。采用现代化微服务架构设计,结合多 LLM 支持、实时协作和复杂分析能力,提供可操作的洞察,显著提升代码质量。

**愿景**: 通过 AI 智能增强人工评审,赋能开发团队更快地交付高质量代码。

## 🏗️ 架构设计

### 系统架构

AIReview 遵循**分层清洁架构**,具有清晰的关注点分离:

```
┌─────────────────────────────────────────────────────────────┐
│                  前端层 (React)                              │
│  • React 19 + TypeScript + Vite                             │
│  • SignalR 实时更新                                          │
│  • React Query 状态管理                                      │
└────────────────────────┬────────────────────────────────────┘
                         │ REST API / SignalR
┌────────────────────────┴────────────────────────────────────┐
│            API 层 (ASP.NET Core Web API)                    │
│  • 控制器 (认证、项目、评审)                                  │
│  • SignalR Hubs (实时通知)                                   │
│  • 中间件 (认证、错误处理、日志)                              │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   核心业务逻辑层                              │
│  • 领域实体 (Project, Review, User, Prompt 等)              │
│  • 服务接口 (AI, Git, Project, Analysis)                    │
│  • 业务服务 (编排 & 领域逻辑)                                 │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                    基础设施层                                 │
│  • 数据访问 (EF Core + Repository 模式)                      │
│  • 外部服务 (多 LLM 提供商、Git)                             │
│  • 后台作业 (Hangfire 异步分析)                              │
│  • 缓存 & 会话管理 (Redis)                                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                  ┌──────┴──────┐
                  │   数据库     │
                  │ (SQLite/PG) │
                  └──────────────┘
```

### 核心架构原则

1. **关注点分离**: 每层具有明确的职责
   - API 层处理 HTTP/SignalR 通信
   - Core 层包含纯业务逻辑
   - Infrastructure 层管理外部依赖

2. **依赖倒置**: 核心逻辑依赖抽象(接口),而非实现

3. **Repository & Unit of Work 模式**: 统一的数据访问与事务支持

4. **异步处理**: 后台作业处理长时运行的 AI 分析任务

5. **实时通信**: SignalR 实现评审更新的推送通知

### 模块职责

| 模块 | 职责 | 核心组件 |
|--------|---------------|----------------|
| **AIReview.API** | 入口点、请求处理、实时 hub | Controllers, SignalR Hubs, Middleware |
| **AIReview.Core** | 业务规则、领域模型、服务契约 | Entities, Interfaces, Domain Services |
| **AIReview.Infrastructure** | 数据持久化、外部集成、后台作业 | Repositories, EF Core, Hangfire, LLM clients |
| **AIReview.Shared** | 跨领域关注点、DTOs | Data Transfer Objects, Enums |
| **AIReview.Tests** | 质量保证 | Unit tests, Integration tests |
| **aireviewer-frontend** | 用户界面、客户端逻辑 | React components, API clients |

- [中文设计文档](design.md)

## 🚀 核心功能

### 1. **智能 AI 代码评审**
- **多维度分析**: 质量、安全、性能、可维护性评估
- **风险评分系统**: 跨多个维度的自动化风险评估
  - 复杂度分析(圈复杂度、嵌套深度)
  - 安全漏洞检测
  - 性能瓶颈识别
  - 可维护性指标
- **上下文建议**: AI 生成针对性的改进建议
- **多 LLM 支持**: 可配置提供商(OpenAI、Azure OpenAI、自定义模型)
- **异步处理**: 后台作业确保大型代码库的响应式用户体验

### 2. **可定制 Prompt 管理** ⭐ 新功能
- **三层模板系统**: 内置 → 用户级 → 项目级覆盖
- **模板类型**:
  - 代码评审模板
  - 风险分析模板
  - Pull Request 摘要模板
  - 改进建议模板
- **灵活占位符**: {{CONTEXT}}, {{DIFF}}, {{FILE_NAME}} 等
- **UI 管理**: 直观的 Web 界面进行模板 CRUD 操作

### 3. **高级 Pull Request 分析**
- **智能变更摘要**: AI 生成的 PR 描述与影响分析
- **变更类型分类**: 功能、Bug修复、重构、文档等
- **影响评估**:
  - 业务影响评估
  - 技术债务分析
  - 破坏性变更检测
- **测试建议**: 自动生成测试重点领域
- **部署注意事项**: 风险评估与回滚考虑

### 4. **改进建议引擎**
- **分类建议**: 代码质量、性能、安全、架构等
- **优先级评分**: 基于影响和工作量的自动化优先级排序
- **用户反馈循环**: 接受/忽略跟踪以持续改进
- **历史跟踪**: 监控建议接受率随时间的变化

### 5. **Git 集成**
- **仓库导入**: 克隆并分析现有 Git 仓库
- **Diff 分析**: 智能解析代码变更
- **提交历史**: 跟踪评审历史与 Git 提交
- **分支支持**: 无缝处理多个分支

### 6. **项目 & 团队管理**
- **项目组织**: 创建和管理多个评审项目
- **基于角色的访问控制**: Owner、Reviewer、Developer 角色
- **成员管理**: 邀请团队成员并授予细粒度权限
- **项目设置**: 每个项目的配置和偏好

### 7. **实时协作**
- **实时通知**: SignalR 驱动的即时更新
- **评审评论**: 代码变更的线程化讨论
- **状态跟踪**: 实时评审工作流状态更新
- **活动信息流**: 团队活动可见性

### 8. **评审工作流**
- **评审请求管理**: 创建、分配和跟踪评审
- **审批流程**: 批准、拒绝或请求更改
- **状态生命周期**: Pending → In Review → Approved/Rejected
- **评论系统**: 逐行和总体反馈

## 📊 项目截图

![home](images/home.png)

![review](images/review.png)

## 📁 仓库结构

```
AIReview/
├── AIReview.API/              # Web API 入口点
│   ├── Controllers/           # REST API 端点
│   ├── Hubs/                  # SignalR 实时 hubs
│   ├── Services/              # API 层服务
│   └── Program.cs             # 应用配置 & 依赖注入
│
├── AIReview.Core/             # 领域 & 业务逻辑
│   ├── Entities/              # 领域模型 (EF Core 实体)
│   ├── Interfaces/            # 服务契约 & 抽象
│   └── Services/              # 业务逻辑实现
│
├── AIReview.Infrastructure/   # 外部依赖
│   ├── Data/                  # EF Core DbContext & 配置
│   ├── Repositories/          # 数据访问实现
│   ├── Services/              # 外部服务集成
│   ├── BackgroundJobs/        # Hangfire 作业定义
│   └── Migrations/            # EF Core 数据库迁移
│
├── AIReview.Shared/           # 跨领域关注点
│   ├── DTOs/                  # 数据传输对象
│   └── Enums/                 # 共享枚举
│
├── AIReview.Tests/            # 测试套件
│   └── Services/              # 单元 & 集成测试
│
├── aireviewer-frontend/       # React 前端
│   ├── src/
│   │   ├── components/        # 可复用 UI 组件
│   │   ├── pages/             # 路由级页面组件
│   │   ├── services/          # API 客户端服务
│   │   ├── types/             # TypeScript 类型定义
│   │   └── App.tsx            # 根组件 & 路由
│   └── vite.config.ts         # Vite 构建配置
│
└── docs/                      # 文档
    ├── design.md              # 架构设计 (中文)
    ├── design.en-us.md        # 架构设计 (English)
    └── features/              # 功能特性文档
```

## 🛠️ 技术栈

### 后端架构
- **.NET 8.0**: 最新跨平台框架，性能改进
- **ASP.NET Core Web API**: RESTful API，OpenAPI/Swagger 文档
- **Entity Framework Core**: Code-First ORM，迁移支持
- **ASP.NET Core Identity**: 基于 JWT 的认证和授权
- **SignalR**: 基于 WebSocket 的实时双向通信
- **Hangfire**: 后台作业处理异步 AI 分析
- **SQLite/PostgreSQL**: 灵活的数据库选项(开发/生产)
- **Redis**: 分布式缓存和会话管理(可选)

### 前端架构
- **React 19**: 最新 React，并发特性
- **TypeScript**: 类型安全开发，完整 IntelliSense
- **Vite**: 闪电般快速的 HMR 和优化的生产构建
- **TailwindCSS**: 实用优先的 CSS，自定义设计系统
- **React Query (@tanstack/react-query)**: 强大的服务器状态管理
- **React Router v6**: 声明式客户端路由
- **Axios**: 基于 Promise 的 HTTP 客户端，拦截器支持
- **Heroicons**: 精美手工制作的 SVG 图标

### 基础设施 & DevOps
- **Docker**: 容器化一致性环境
- **Docker Compose**: 本地开发的多容器编排
- **GitHub Actions**: CI/CD 流水线(计划中)
- **Kubernetes**: 生产编排(计划中)

## ⚙️ 前置依赖

- **.NET SDK 8.0+**: [下载](https://dotnet.microsoft.com/download)
- **Node.js 18+** 和 **npm/pnpm**: [下载](https://nodejs.org/)
- **SQLite**(默认)或 **PostgreSQL 14+**(生产环境可选)
- **Redis**(可选,用于分布式缓存): [下载](https://redis.io/download)
- **Docker Desktop**(可选,用于容器化开发): [下载](https://www.docker.com/products/docker-desktop)

## 🚀 快速开始

### 后端配置

1. **克隆仓库**:
   ```bash
   git clone https://github.com/wosledon/AIReview.git
   cd AIReview
   ```

2. **配置应用设置**: 编辑 `AIReview.API/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=aireviewer.db",  // SQLite
       "Redis": "localhost:6379"
     },
     "Jwt": {
       "Secret": "你的JWT密钥（至少32字符）",
       "Issuer": "AIReview",
       "Audience": "AIReview"
     }
   }
   ```

3. **执行数据库迁移**:
   ```bash
   cd AIReview.API
   dotnet ef database update
   ```

4. **启动 API 服务**:
   ```bash
   dotnet run
   # API 将运行在 http://localhost:5000
   # Swagger UI: http://localhost:5000/swagger
   ```

### 前端配置

1. **安装依赖**:
   ```bash
   cd aireviewer-frontend
   npm install
   ```

2. **配置环境变量**: 创建 `.env` 文件:
   ```env
   VITE_API_BASE_URL=http://localhost:5000/api/v1
   ```

3. **启动开发服务器**:
   ```bash
   npm run dev
   # 前端将运行在 http://localhost:5173
   ```

## 🧪 测试

### 后端测试
```bash
cd AIReview.Tests
dotnet test --verbosity normal
```

### 前端测试
```bash
cd aireviewer-frontend
npm run test  # 根据需要配置 Vitest/Jest
```

## 📦 部署

### Docker 部署

1. **构建 Docker 镜像**:
   ```bash
   # 后端
   docker build -t aireviewer-api -f AIReview.API/Dockerfile .
   
   # 前端
   docker build -t aireviewer-frontend -f aireviewer-frontend/Dockerfile .
   ```

2. **使用 Docker Compose 运行**:
   ```bash
   docker-compose up -d
   ```

### 生产环境考虑事项
- 生产环境使用 PostgreSQL 而非 SQLite
- 配置 Redis 用于分布式缓存和会话管理
- 使用反向代理(Nginx/Traefik)设置 HTTPS
- 仅为受信任的来源启用 CORS
- 使用环境变量存储机密(切勿提交凭据)
- 配置日志和监控(Application Insights, Serilog 等)
- 设置数据库备份策略

## 🔒 安全

- **认证**: 基于 JWT，支持刷新令牌
- **授权**: 基于角色的访问控制(RBAC)
- **数据保护**: ASP.NET Core Data Protection 保护敏感数据
- **输入验证**: 模型验证和清理
- **速率限制**: API 限流防止滥用
- **HTTPS**: 生产环境强制 TLS 1.2+
- **CORS**: 限制为允许的来源
- **SQL 注入**: 通过 EF Core 参数化查询

## 🗺️ 路线图 & 未来规划

### 🔥 近期规划 (未来 3-6 个月)

#### 增强 AI 能力
- [ ] **高级代码修复建议**: AI 生成的代码补丁与差异预览
- [ ] **多模型集成**: 结合多个 LLM 响应以提高准确性
- [ ] **上下文感知分析**: 从历史评审中学习以改进建议
- [ ] **自定义 AI 模型微调**: 支持组织特定的模型训练

#### 集成生态
- [ ] **GitHub/GitLab Webhooks**: PR 创建时自动触发评审
- [ ] **VS Code 扩展**: IDE 中的内联代码评审和建议
- [ ] **Slack/Teams 通知**: 与团队沟通工具集成
- [ ] **CI/CD 流水线集成**: 基于 AI 评审分数的质量门

#### 分析 & 报告
- [ ] **评审分析仪表板**: 团队生产力和代码质量指标
- [ ] **趋势分析**: 跟踪质量改进随时间的变化
- [ ] **自定义报告**: 可导出的管理报告
- [ ] **开发者性能洞察**: 个人贡献质量指标

### 🎯 中期规划 (6-12 个月)

#### 团队学习 & 适应
- [ ] **团队编码标准学习**: AI 从接受/拒绝的建议中学习
- [ ] **自定义规则引擎**: 定义组织特定的编码标准
- [ ] **评审模板库**: 跨团队共享模板
- [ ] **自动化风格指南执行**: 自动执行团队约定

#### 性能 & 可扩展性
- [ ] **增量分析**: 仅分析大文件的变更部分
- [ ] **批量评审处理**: 高效处理多个 PR
- [ ] **分布式处理**: 分析工作负载的水平扩展
- [ ] **缓存优化**: 减少冗余的 AI 调用

#### 高级功能
- [ ] **代码安全扫描**: 深度安全漏洞分析
- [ ] **许可证合规检查器**: 检测依赖项中的许可问题
- [ ] **架构违规检测**: 强制执行架构模式
- [ ] **技术债务跟踪器**: 量化和优先排序技术债务

### � 长期愿景 (12+ 个月)

#### 企业能力
- [ ] **多租户架构**: 完整的 SaaS 支持，数据隔离
- [ ] **企业 SSO**: SAML, OAuth, LDAP 集成
- [ ] **审计日志**: 全面的合规性和审计追踪
- [ ] **高级访问控制**: 细粒度权限和策略
- [ ] **本地部署**: 离线企业部署选项

#### AI 演进
- [ ] **自动化代码重构**: AI 建议并应用重构
- [ ] **预测性 Bug 检测**: ML 模型预测易出错代码
- [ ] **测试生成**: 为评审的代码自动生成单元测试
- [ ] **文档生成**: AI 编写的内联文档

#### 平台扩展
- [ ] **移动应用**: iOS 和 Android 应用，随时随地评审
- [ ] **API 市场**: 第三方集成和扩展
- [ ] **社区插件系统**: 自定义分析器的开放生态系统
- [ ] **多语言支持**: 全球团队的本地化

#### 机器学习优化
- [ ] **持续模型改进**: 从大规模用户反馈中学习
- [ ] **自定义模型市场**: 共享和下载专业模型
- [ ] **迁移学习**: 将预训练模型适配到特定领域
- [ ] **可解释 AI**: AI 决策的透明度

## 💡 为什么选择 AIReview?

- **🎯 准确性**: 多 LLM 支持和可定制 prompt 确保相关建议
- **⚡ 速度**: 异步处理和缓存使评审快速
- **🔧 灵活性**: 用户和项目级别的广泛定制
- **👥 协作**: 实时更新使团队保持同步
- **📈 可扩展性**: 基于经过验证的企业技术(.NET, React)
- **🔒 安全**: 基于角色的访问控制和安全认证
- **🌐 开源**: MIT 许可证鼓励社区贡献

## 🤝 贡献

欢迎贡献!请查看我们的[贡献指南](CONTRIBUTING.md)(即将推出)了解详情:
- 行为准则
- 开发工作流程
- Pull Request 流程
- 编码标准

## 📄 许可

本项目采用 MIT 许可证 - 详见 [LICENSE](../LICENSE) 文件。

## 📚 文档

- [Architecture Design (English)](design.en-us.md)
- [架构设计 (中文)](design.md)
- [English README](../README.md)
- [功能文档](features/)

## 📧 联系 & 支持

- **Issues**: [GitHub Issues](https://github.com/wosledon/AIReview/issues)
- **Discussions**: [GitHub Discussions](https://github.com/wosledon/AIReview/discussions)

## 🙏 致谢

用 ❤️ 构建，使用现代开源技术。特别感谢 .NET、React 和 AI 社区。

---

**如果您觉得有用，请给这个仓库点个 Star ⭐!**
