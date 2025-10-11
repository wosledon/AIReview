AIReview — AI 驱动的代码/文档评审平台
=================================

简体中文 | [English](./../README.md)

## 概述

AIReview 是一款面向团队的智能评审平台，支持对代码与文档进行分析，自动生成可执行的评审意见，并与 Git 流程与 IDE 无缝集成，显著提升评审效率与质量。

主要能力：
- 自动化代码质量与风险检测
- AI 生成评审见解与修复建议
- 支持多语言代码/文档
- 集成 Git 平台与 IDE
- 可配置的评审流程与实时通知
- 团队协作、权限与审计

架构与模块职责、流程等高层设计请参阅：
- 英文设计：docs/design.en-us.md
- 中文设计：docs/design.md

## 概览

![home](./../docs/images/home.png)

![review](./../docs/images/review.png)

## 仓库结构

- AIReview.API：ASP.NET Core Web API（后端）
- AIReview.Core：领域模型、服务与接口
- AIReview.Infrastructure：EF Core、仓储、迁移、后台作业
- AIReview.Shared：共享 DTO 与枚举
- AIReview.Tests：测试工程
- aireviewer-frontend：Web 前端（Vite + TypeScript）
- docs：设计与文档

## 前置依赖

- .NET SDK 8.0+
- Node.js 18+ 与 pnpm/npm（用于前端）
- PostgreSQL 14+（或兼容版本）
- 可选：Redis、Docker Desktop、K8s 工具链

## 后端运行（API）

1) 在 AIReview.API 中配置 appsettings.Development.json（数据库连接串、JWT、LLM 供应商配置等）。
2) 执行数据库迁移。
3) 启动 API 服务。

环境配置要点：
- 数据库：Host=localhost; Database=ai_review; Username=...; Password=...
- JWT：issuer、audience、签名密钥
- LLM：供应商、模型、API Key（妥善保管）

## 前端运行（Web）

1) 进入 aireviewer-frontend 目录
2) 安装依赖
3) 启动开发服务

请在前端环境（如 .env 或 vite 配置）中设置 API 基础地址，指向后端服务。

## 测试

- 后端：将单元/集成测试放置于 AIReview.Tests，并使用 .NET 测试工具运行。
- 前端：可选用 Vitest 等框架，并通过脚本运行。

## 部署

- 使用多阶段 Docker 镜像构建前后端
- 使用 Kubernetes 进行编排（Deployment/Service/Ingress）
- 通过环境变量或密钥管理器外部化配置与密钥
- 配置指标、日志与追踪以提升可观测性

## 安全

- 基于 JWT/OIDC 的认证与基于角色/资源的授权
- 输入校验、输出编码与速率限制
- 传输与静态加密；密钥安全存储

## 路线图（高层）

- GitHub/GitLab 集成，VS Code 等 IDE 插件
- 高级 AI 建议与自动修复提案
- 团队偏好学习与规则自适应
- 多租户与企业策略控制

## 许可

待定。若对外贡献或内部落地，请与组织的许可与合规策略保持一致。

## 链接

- 高层设计（中文）：docs/design.md
- High-level design (EN): docs/design.en-us.md
- English README: README.md
