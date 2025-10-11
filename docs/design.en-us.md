# AI Review Platform — High‑Level Design

English | [简体中文](design.md)

This document focuses on high-level architecture, module responsibilities, interfaces, and workflows. It intentionally excludes any concrete implementation code or configuration snippets (e.g., C#/SQL/Docker/K8s).

## 1. Overview

### 1.1 Goals
Build an intelligent platform for reviewing code and documents. Leverage AI to improve review efficiency and quality, reduce manual workload, and integrate seamlessly with existing team workflows.

### 1.2 Core Capabilities
- Automated code quality and risk detection
- AI-generated review comments and actionable suggestions
- Multi-language support for code and docs
- Seamless integration with Git platforms and IDEs
- Configurable review workflow and state transitions
- Collaboration (comments, assignment, notifications) and audit trail

## 2. System Architecture

### 2.1 System Overview
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │    API Gateway   │    │     AI Layer     │
│   Web/IDE Ext   │◄──►│  AuthN/AuthZ     │◄──►│  Code Analysis    │
│                 │    │  Routing         │    │  AI Review       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
																▲                        ▲
																│                        │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Data Storage  │    │   Biz Logic     │    │  Integrations    │
│   PostgreSQL    │◄──►│  Review/Project │◄──►│  Git Platforms    │
│   Redis Cache   │    │  User/Access    │    │  CI/CD, Notify   │
│   Object Store  │    │  Notify/Jobs    │    │                   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 2.2 Technology Stack

#### 2.2.1 Backend
- Language: C# (.NET 8+)
- Framework: ASP.NET Core Web API
- Database: PostgreSQL (primary) + Redis (cache)
- ORM: Entity Framework Core
- MQ: RabbitMQ / Azure Service Bus (optional)
- Search: Elasticsearch (optional)
- Container & Orchestration: Docker + Kubernetes

#### 2.2.2 Frontend
- Framework: React 18+ / Vue 3+
- State: Redux Toolkit / Pinia
- UI: Ant Design / Element Plus
- Editor: Monaco Editor

#### 2.2.3 AI/ML
- Code analysis: AST/Tree-sitter/regex rules
- LLMs: OpenAI/Claude/open-source (pluggable)
- Embeddings: CodeBERT/GraphCodeBERT (optional)

## 3. Core Modules

### 3.1 Code Analysis Engine

Responsibilities:
- Parse multi-language source to extract syntax/structure
- Execute static rules and heuristics
- Output normalized issue items with severity, category, location, and suggestions

Inputs/Outputs (abstract):
- Input: source/diff, language, context (project rules/thresholds)
- Output: analysis result (issue list, metrics, risk distribution)

Rules (examples):
- Quality: cyclomatic complexity threshold, long functions/files, duplication, dead code, unused imports, naming/style
- Security: input validation gaps leading to SQLi/XSS, hard-coded secrets/credentials, weak randomness/crypto
- Performance: N+1 queries, inefficient collections, unnecessary sync I/O/blocking

Extensibility:
- Configurable rule sets per project/team/language
- Support local rules and remote policy distribution

### 3.2 AI Review Engine

Responsibilities:
- Build context (diff, key file snippets, historical issues, project policies)
- Invoke LLM to produce review insights, risk rationale, and fix suggestions
- Parse and structure LLM output (score, summary, actionable items, comments)

Key design points:
- Context trimming and window management (priority, deduplication)
- Prompt strategy (roles, tasks, format, constraints, safety)
- Output schema (e.g., JSON-like), tolerant parsing and graceful degradation

Result structure (abstract):
- Overall score (0–100)
- Comments: file/line, severity (info/warn/error), category (quality/security/performance/style), suggestion
- Summary and Actionable Items

### 3.3 Review Workflow

States: Pending → AIReviewing → HumanReview → Approved/Rejected → Merged

Transitions:
- Pending → AIReviewing only
- After AIReviewing, move to HumanReview; failures can retry or return to Pending
- HumanReview → Approved or Rejected
- Approved → Merged after compliance checks

Triggers:
- Git Push/MR(PR) events
- Scheduled jobs (project-wide periodic scans)
- Manual triggers

## 4. Data Model (Conceptual)

Core entities:
- Project: name, description, repo URL, language, timestamps
- ProjectMember: project, user, role (owner/admin/developer/viewer)
- ReviewRequest: project, author, title, description, branch, base branch, status
- ReviewComment: request, author, file path, line number, content, severity, category, is AI generated
- LLMConfiguration: provider, model, temperature, quotas, secret reference

Relationships:
- Project 1–N ReviewRequest
- Project 1–N ProjectMember
- ReviewRequest 1–N ReviewComment

Indexing suggestions:
- ReviewRequest(project_id, status)
- ReviewComment(review_request_id, file_path)
- ReviewComment(author_id)

## 5. API Design (Endpoint Overview)

Namespace: `/api/v1`

- Projects
	- GET `/projects`
	- POST `/projects`
	- GET `/projects/{id}`
	- PUT `/projects/{id}`
	- DELETE `/projects/{id}`
	- GET `/projects/{id}/members`
	- POST `/projects/{id}/members`
	- DELETE `/projects/{id}/members/{userId}`

- Reviews
	- GET `/reviews`
	- POST `/reviews`
	- GET `/reviews/{id}`
	- PUT `/reviews/{id}`
	- DELETE `/reviews/{id}`
	- POST `/reviews/{id}/ai-review` (async)
	- GET `/reviews/{id}/ai-result`
	- GET `/reviews/{id}/comments`
	- POST `/reviews/{id}/comments`

- Other resources (LLM Configuration / Auth / Git integrations) follow standard REST semantics.

Real-time (SignalR):
- Events: review state changes, AI job progress, comment updates, merge status
- Client rooms: by project/review request

## 6. Integrations

### 6.1 Git Platforms
- GitHub/GitLab via OAuth/App; subscribe to Push/MR/comment webhooks
- Sync PR/MR with internal ReviewRequest states and comments

### 6.2 IDE Extensions (e.g., VS Code)
- One-click review, inline comments/suggestions, quick fixes
- Identity binding via platform token/OAuth

## 7. Deployment & Operations (High Level)

Environments: Dev / Staging / Prod

Containers & Orchestration:
- Multi-stage images
- Kubernetes Deployment/Service/Ingress

Configuration & Secrets:
- Env/config store (DB strings, JWT, LLM secrets)
- Secret management (K8s Secret/Azure Key Vault)

Observability:
- Metrics: review volume, AI call latency/throughput, error rate
- Logs: structured logs (requests/audit/slow queries)
- Tracing: distributed tracing (optional)

## 8. Security & Compliance

AuthZ/AuthN:
- JWT/OIDC with role- and resource-scoped permissions
- Fine-grained access (project roles + review-level checks)

Data security:
- TLS in transit; at-rest encryption
- Input validation and output encoding; rate limiting
- Abuse prevention and audit logging

## 9. Performance & Availability

Caching:
- Hot lists and details (short TTL)
- Read caching for review results and comments

Asynchrony:
- AI review jobs queued, idempotent retries
- Git sync and indexing as background tasks

Scalability:
- Horizontal scale for API and AI workers
- Read/write separation (optional) and resilience (circuit breakers, fallbacks)

## 10. Testing Strategy

- Unit: rules, parsing/formatting, permission checks
- Integration: API suites (auth, boundaries), DB migrations
- E2E (optional): Push/MR to platform comments loop
- Regression: core flows and high-risk paths

## 11. Evolution & Extensions

- Smart code completion and auto-fix proposals
- Team preference learning and adaptive rules
- Multimodal (docs, configs, architecture diagrams)
- Multi-tenant and enterprise policy center

## 12. Milestones

Phase 1: Core (2–3 months)
- Foundation and auth
- Code analysis and basic rules
- AI review MVP and frontend views

Phase 2: Integration & Optimization (1–2 months)
- Git integration and IDE extension
- Performance and stability hardening
- Test coverage and CI improvements

Phase 3: Advanced (1–2 months)
- Real-time collaboration
- Advanced AI features and reporting
- Enterprise features and compliance

## 13. Key Success Factors

1) Accurate, stable, and explainable AI review
2) Seamless integration with Git/IDE workflows
3) Performance and reliability (SLA/SLO)
4) Continuous iteration driven by user feedback

— End —
