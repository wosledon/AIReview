AIReview — AI-powered Code/Doc Review Platform
================================================

English | [简体中文](docs/Readme.zh-cn.md)

## Overview

AIReview is an AI-assisted review platform that analyzes code and documents, generates actionable review comments, and integrates with your Git workflow and IDEs. It aims to boost review efficiency and quality while reducing manual workload.

Key capabilities:
- Automated code quality and risk detection
- AI-generated review insights and fix suggestions
- Multi-language code/doc support
- Git platform and IDE integrations
- Configurable review workflow with real-time updates
- Team collaboration, permissions, and audit trail

For high-level architecture, module responsibilities, and workflows, see:
- English design: docs/design.en-us.md
- Chinese design: docs/design.md

## Repository Structure

- AIReview.API: ASP.NET Core Web API (backend)
- AIReview.Core: Domain models, services, and interfaces
- AIReview.Infrastructure: EF Core, repositories, migrations, background jobs
- AIReview.Shared: Shared DTOs and enums
- AIReview.Tests: Test projects
- aireviewer-frontend: Web frontend (Vite + TypeScript)
- docs: Design and documentation

## Prerequisites

- .NET SDK 8.0+
- Node.js 18+ and pnpm/npm (for frontend)
- PostgreSQL 14+ (or a compatible version)
- Optional: Redis, Docker Desktop, Kubernetes tooling

## Backend Setup (API)

1) Configure appsettings.Development.json in AIReview.API with your PostgreSQL connection string, JWT, and any LLM provider settings.
2) Apply database migrations.
3) Run the API.

Example environment notes:
- Database: Host=localhost; Database=ai_review; Username=...; Password=...
- JWT: issuer, audience, signing key
- LLM: provider name, model id, api key (store securely)

## Frontend Setup (Web)

1) cd aireviewer-frontend
2) Install dependencies
3) Start the dev server

Configure the API base URL in the frontend environment (e.g., .env or vite config) to point to the backend.

## Tests

- Backend: place unit/integration tests under AIReview.Tests and run via the .NET test runner.
- Frontend: add tests using your preferred framework (e.g., Vitest) and run via package scripts.

## Deployment

- Containerize the API and frontend with multi-stage Docker builds
- Orchestrate with Kubernetes (Deployment/Service/Ingress)
- Externalize configuration and secrets via environment variables or a secret manager
- Set up metrics, logs, and tracing as needed

## Security

- JWT/OIDC-based auth with role- and resource-scoped permissions
- Input validation, output encoding, and rate limiting
- Encrypt data in transit and at rest; store secrets securely

## Roadmap (high level)

- GitHub/GitLab integration, IDE extensions (VS Code)
- Advanced AI suggestions and auto-fix proposals
- Team preference learning and rules adaptation
- Multi-tenant and enterprise policy controls

## License

TBD. If contributing or deploying internally, align with your organization’s licensing and compliance policies.

## Links

- High-level design (EN): docs/design.en-us.md
- 高层设计（中文）: docs/design.md
- 中文 README：docs/Readme.zh-cn.md
