AIReview — AI 驱动的代码/文档评审平台
=================================

简体中文 | [English](../README.md)

## 概述

AIReview 是一款面向团队的智能代码评审平台，利用AI技术自动分析代码质量，生成评审建议，并与Git工作流程无缝集成，显著提升代码评审效率与质量。

主要功能：
- **AI驱动的代码评审**：自动分析代码质量、安全风险和最佳实践
- **Git集成**：支持项目导入和代码差异分析
- **实时协作**：通过SignalR实现实时通知和评论
- **多LLM支持**：可配置不同的AI模型提供商
- **项目管理**：支持项目创建、成员管理和权限控制
- **评审工作流**：支持评审状态管理、批准/拒绝流程

架构与模块职责、流程等高层设计请参阅：
- 英文设计：docs/design.en-us.md
- 中文设计：docs/design.md

## 项目截图

![home](images/home.png)

![review](images/review.png)

## 仓库结构

- **AIReview.API**：ASP.NET Core Web API后端，包含控制器、Hub和服务配置
- **AIReview.Core**：核心业务逻辑，包含实体模型、服务接口和业务服务
- **AIReview.Infrastructure**：基础设施层，包含EF Core数据访问、仓储模式和后台作业
- **AIReview.Shared**：共享数据传输对象(DTO)和枚举定义
- **AIReview.Tests**：单元测试和集成测试
- **aireviewer-frontend**：React + TypeScript前端应用，使用Vite构建
- **docs**：项目文档和设计说明

## 技术栈

### 后端
- **.NET 8.0**：现代化的跨平台应用开发框架
- **ASP.NET Core Web API**：RESTful API服务
- **Entity Framework Core**：对象关系映射(ORM)
- **ASP.NET Core Identity**：用户认证和授权
- **SignalR**：实时双向通信
- **SQLite**：轻量级数据库（可配置PostgreSQL）
- **后台作业**：异步AI评审处理

### 前端
- **React 19**：现代化UI框架  
- **TypeScript**：类型安全的JavaScript
- **Vite**：快速的前端构建工具
- **TailwindCSS**：实用优先的CSS框架
- **React Query**：服务器状态管理
- **React Router**：客户端路由
- **Axios**：HTTP客户端

## 前置依赖

- .NET SDK 8.0+
- Node.js 18+ 与 npm/pnpm（用于前端）
- SQLite（默认）或 PostgreSQL 14+（可选）
- 可选：Redis（用于缓存和会话）、Docker Desktop

## 后端配置与运行

1. **配置应用设置**：在 `AIReview.API/appsettings.Development.json` 中配置：
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
     },
     "LLMProviders": {
       // 配置AI模型提供商
     }
   }
   ```

2. **执行数据库迁移**：
   ```bash
   cd AIReview.API
   dotnet ef database update
   ```

3. **启动API服务**：
   ```bash
   dotnet run
   ```

## 前端配置与运行

1. **安装依赖**：
   ```bash
   cd aireviewer-frontend
   npm install
   ```

2. **配置环境变量**：创建 `.env` 文件：
   ```
   VITE_API_BASE_URL=http://localhost:5000
   ```

3. **启动开发服务器**：
   ```bash
   npm run dev
   ```

## 核心功能

### 项目管理
- 创建和管理代码评审项目
- 团队成员邀请和权限管理
- Git仓库集成和项目迁移

### AI代码评审
- 支持多种LLM提供商配置
- 自动代码质量分析和风险检测
- 生成详细的评审建议和修复方案
- **AI驱动的PR总结**：智能分析代码变更，生成结构化的变更摘要
- 异步处理确保响应性能

### 实时协作
- SignalR实时通知系统
- 评审评论和讨论
- 状态变更实时推送

### 评审工作流
- 评审请求创建和管理
- 批准/拒绝/请求修改流程
- 代码差异查看和分析

## 测试

### 后端测试
```bash
cd AIReview.Tests
dotnet test
```

### 前端测试
```bash
cd aireviewer-frontend
npm run test  # 如果配置了测试框架
```

## 部署

### Docker部署
1. 构建镜像：
   ```bash
   docker build -t aireviewer-api ./AIReview.API
   docker build -t aireviewer-frontend ./aireviewer-frontend
   ```

2. 运行容器：
   ```bash
   docker run -d -p 5000:80 aireviewer-api
   docker run -d -p 3000:80 aireviewer-frontend
   ```

### 生产环境配置
- 使用环境变量外部化敏感配置
- 配置HTTPS和安全头
- 设置数据库连接池和缓存
- 配置日志记录和监控

## 安全

- 基于 JWT/OIDC 的认证与基于角色/资源的授权
- 输入校验、输出编码与速率限制
- 传输与静态加密；密钥安全存储

## 开发路线图

### 近期计划
- [ ] **PR智能总结功能**
  - AI生成Pull Request变更摘要和影响分析
  - 自动识别代码变更类型（功能增强、Bug修复、重构等）
  - 生成变更风险评估和建议的测试重点
  - 支持多语言代码变更的语义分析
- [ ] GitHub/GitLab Webhook集成
- [ ] VS Code插件开发
- [ ] 更多AI模型支持（Claude、GPT-4等）
- [ ] 代码自动修复建议

### 中期计划  
- [ ] 团队编码规范学习和适应
- [ ] 批量评审和批处理优化
- [ ] 评审报告和统计分析
- [ ] 移动端应用支持

### 长期愿景
- [ ] 多租户架构支持
- [ ] 企业级权限和策略控制
- [ ] 机器学习模型优化
- [ ] 开源社区生态建设

## 许可

本项目采用 MIT 许可证 - 详情请见 [LICENSE](../LICENSE) 文件。
