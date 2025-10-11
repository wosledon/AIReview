# AI评审平台设计方案（高层设计）

[English](design.en-us.md) | 简体中文

本文档聚焦于架构、模块职责、接口与流程等高层设计，明确边界与约束，不包含任何具体实现代码或配置清单（如 C#/SQL/Dockerfile 等）。

## 1. 项目概述

### 1.1 项目目标
构建一个智能化的代码/文档评审平台，利用 AI 技术提升评审效率与质量，降低人工评审负担，并与团队现有的开发流程无缝集成。

### 1.2 核心功能
- 自动化代码质量检测与风险识别
- 智能评审意见与可执行建议生成
- 多语言代码与文档支持
- 与 Git 平台与 IDE 的无缝集成
- 可配置的评审流程与状态流转
- 团队协作（评论、指派、通知）与审计

## 2. 系统架构设计

### 2.1 整体架构
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   前端界面      │    │   API 网关      │    │   AI 服务层      │
│   Web/IDE 插件  │◄──►│   认证/授权     │◄──►│   代码分析       │
│                 │    │   路由分发      │    │   智能评审       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                ▲                        ▲
                                │                        │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   数据存储层    │    │   业务逻辑层    │    │   外部集成       │
│   PostgreSQL    │◄──►│   评审/项目     │◄──►│   Git 平台       │
│   Redis 缓存     │    │   用户/权限     │    │   CI/CD 工具     │
│   对象存储      │    │   通知/任务     │    │   通知服务       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 2.2 技术栈选择

#### 2.2.1 后端
- 编程语言：C#（.NET 8+）
- 框架：ASP.NET Core Web API
- 数据库：PostgreSQL（主）+ Redis（缓存）
- ORM：Entity Framework Core
- 消息队列：RabbitMQ / Azure Service Bus（可选）
- 搜索：Elasticsearch（可选，代码索引/检索）
- 容器化：Docker + Kubernetes

#### 2.2.2 前端
- 框架：React 18+ / Vue 3+
- 状态管理：Redux Toolkit / Pinia
- UI 组件：Ant Design / Element Plus
- 代码编辑器：Monaco Editor

#### 2.2.3 AI/ML
- 代码分析：AST/Tree-sitter/Regex 规则
- 模型：OpenAI/Claude/开源 LLM（可插拔）
- 嵌入：CodeBERT/GraphCodeBERT（可选）

## 3. 核心模块设计

### 3.1 代码分析引擎

职责：
- 解析多语言源码，抽取语法/结构信息
- 执行静态规则集与启发式检查
- 输出规范化的“问题项”集合，包含严重级别、类别、定位与建议

输入/输出（抽象）：
- 输入：源码/差异（diff）、语言、上下文（项目规则/阈值）
- 输出：分析结果（问题项列表、统计指标、风险分布）

检测规则（示例）：
- 代码质量：
  - 圈复杂度阈值（例如 >10）
  - 函数/文件长度阈值（例如 >50 行）
  - 重复代码/死代码/未使用导入
  - 命名/风格/约定一致性
- 安全：
  - 输入未校验导致的 SQL 注入/XSS
  - 硬编码密钥/凭据泄露
  - 弱随机数/不安全密码学原语
- 性能：
  - N+1 查询/低效集合操作
  - 不必要的同步 I/O/阻塞调用

扩展性：
- 规则引擎可配置（按项目/团队/语言）
- 支持本地规则与远程策略下发

### 3.2 AI 评审引擎

职责：
- 构建上下文（变更 diff、关键文件片段、历史问题、项目规则）
- 调用 LLM 生成评审意见、风险解释与修复建议
- 解析与结构化 LLM 输出（评分、摘要、可执行条目、评论条目）

关键设计：
- 上下文裁剪与窗口管理（段落/片段优先级、引用去重）
- 提示词策略（角色、任务、格式、约束、安全红线）
- 输出模式（JSON/标记化），容错解析与降级策略

结果结构（抽象）：
- 整体评分（0–100）
- 评论集合：行号/文件路径、严重级别（info/warn/error）、类别（quality/security/performance/style）、建议
- 摘要与可执行清单（Actionable Items）

### 3.3 评审工作流

状态：Pending → AIReviewing → HumanReview → Approved/Rejected → Merged

流转约束：
- Pending 仅能进入 AIReviewing
- AIReviewing 完成后可进入 HumanReview；失败则回到 Pending 或重试
- HumanReview 可进入 Approved 或 Rejected
- Approved 在合规通过后可进入 Merged

触发器：
- Git Push/MR（PR）事件自动触发
- 定时任务（项目级周期扫描）
- 手动触发（按需）

## 4. 数据模型设计（概念）

核心实体：
- Project（项目）：名称、描述、仓库地址、语言、时间戳
- ProjectMember（成员）：项目、用户、角色（owner/admin/developer/viewer）
- ReviewRequest（评审请求）：项目、作者、标题、描述、分支、基线分支、状态
- ReviewComment（评审评论）：请求、作者、文件、行号、内容、严重级别、类别、是否 AI 生成
- LLMConfiguration（模型配置）：供应商、模型、温度、限额、密钥引用

关系：
- Project 1–N ReviewRequest
- Project 1–N ProjectMember
- ReviewRequest 1–N ReviewComment

索引建议：
- ReviewRequest(project_id, status)
- ReviewComment(review_request_id, file_path)
- ReviewComment(author_id)

## 5. API 设计（端点概览）

命名空间：`/api/v1`

- Projects
  - GET `/projects` 列表
  - POST `/projects` 创建
  - GET `/projects/{id}` 详情
  - PUT `/projects/{id}` 更新
  - DELETE `/projects/{id}` 删除
  - GET `/projects/{id}/members` 成员列表
  - POST `/projects/{id}/members` 添加成员
  - DELETE `/projects/{id}/members/{userId}` 移除成员

- Reviews
  - GET `/reviews` 查询（支持过滤/分页）
  - POST `/reviews` 创建
  - GET `/reviews/{id}` 详情
  - PUT `/reviews/{id}` 更新
  - DELETE `/reviews/{id}` 删除
  - POST `/reviews/{id}/ai-review` 触发 AI 评审（异步）
  - GET `/reviews/{id}/ai-result` 获取 AI 结果
  - GET `/reviews/{id}/comments` 获取评论
  - POST `/reviews/{id}/comments` 新增评论

- LLM Configuration / Auth / Git 集成等端点按照资源 REST 语义设计（略）。

实时通信（SignalR）：
- 事件：评审状态变更、AI 任务进度、评论新增/更新、合并状态变更
- 客户端订阅房间：按项目/评审请求维度

## 6. 外部集成

### 6.1 Git 平台
- GitHub/GitLab：接入 OAUTH/APP，订阅 Push/MR/评论 Webhook
- 同步 PR/MR 与平台内 ReviewRequest 的状态与评论

### 6.2 IDE 插件（VS Code 等）
- 主要能力：一键触发评审、在编辑器内查看评论与建议、内联修复建议
- 身份绑定：使用平台 Token/OAuth

## 7. 部署与运维（高层）

环境：Dev / Staging / Prod

容器化与编排：
- 使用多阶段构建镜像
- 通过 Kubernetes 部署 Deployment/Service/Ingress

配置与密钥：
- 配置中心或环境变量（连接串、JWT、LLM 密钥）
- 使用密钥管理（K8s Secret/Azure Key Vault）

可观测性：
- 指标：评审请求总量、AI 调用次数/时延、错误率
- 日志：结构化日志（请求/审计/慢查询）
- 追踪：分布式追踪（可选）

## 8. 安全与合规

认证授权：
- JWT/OIDC，基于角色与资源范围的访问控制
- 细粒度权限（项目成员角色 + 评审级别授权）

数据安全：
- 传输与静态加密（TLS、静态密钥加密）
- 输入校验与输出编码，防注入与 XSS
- 速率限制与防滥用
- 审计日志与合规留痕

## 9. 性能与可用性

缓存策略：
- 热点列表与详情（短 TTL）
- 评审结果与评论的只读缓存

异步化：
- AI 评审任务后台队列化处理，幂等重试
- Git 同步与索引任务异步

扩展性：
- 水平扩展 API 与 AI 工作者
- 读写分离（可选）与服务熔断/降级

## 10. 测试策略

- 单元测试：规则函数、解析与格式化、权限判定
- 集成测试：API 套件（鉴权/鉴权失败/边界）、数据库迁移
- 端到端（可选）：从 Push/MR 到平台评论闭环
- 回归测试：核心流程与高风险路径

## 11. 演进与扩展

- 智能代码补全与自动修复（建议→补丁提案）
- 团队偏好学习与规则自适应
- 多模态（文档、配置、图像架构图）
- 多租户与企业策略中心

## 12. 实施计划（里程碑）

阶段一：核心能力（2–3 个月）
- 基础架构与鉴权
- 代码分析与基础规则
- AI 评审初版与前端展示

阶段二：集成与优化（1–2 个月）
- Git 集成与 IDE 插件
- 性能优化与稳定性提升
- 测试体系完善

阶段三：高级能力（1–2 个月）
- 实时协作
- 高级 AI 功能与报告
- 企业能力与合规

## 总结

AI评审平台的构建需要综合考虑技术架构、AI能力、用户体验和业务需求。通过模块化设计和渐进式开发，可以构建出一个既实用又可扩展的AI评审平台。

关键成功因素：
1. **准确的AI评审能力**: 这是平台的核心价值
2. **良好的集成体验**: 无缝集成到现有开发流程
3. **高性能和稳定性**: 确保平台可靠运行
4. **持续优化**: 根据用户反馈不断改进

这个设计方案提供了一个完整的技术路线图，可以根据具体需求和资源情况进行调整和优化。
