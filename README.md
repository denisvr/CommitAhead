# CommitAhead

CommitAhead is a private, invite-only web application for structured software-engineering interview preparation. It combines a ranked study queue with professional-profile, job-analysis, and interview evidence so the user can answer: **what should I study next, and why?**

## Status

**Phase 0A only** (solution skeleton and architecture baseline) is implemented — not the rest of Phase 0. There is no database, Supabase project, authentication, or domain layer yet. See `docs/roadmap.md` for the full Phase 0 scope still remaining and `docs/prompts/phase-0a-claude-kickoff.md` for what Phase 0A specifically covered.

## Local Requirements

- .NET SDK `10.0.302` (pinned in `backend/global.json`)
- Node.js `24` (pinned in `frontend/.nvmrc`)

```bash
cd backend && dotnet build && dotnet test
cd frontend && npm ci && npm run lint && npm test && npm run build
```

## MVP

- Unified StudyItems for Theory, LeetCode, System Design, and Behavioral preparation
- Transparent ranking from Importance, Demand, and Mastery gap
- Canonical ProfessionalProfile with market-specific CVPresentations and export
- JobAnalysis and InterviewNote evidence sources
- Explicit EvidenceLinks that explain Demand
- Three user-triggered AI analyses that produce drafts requiring human confirmation

The app does not recreate LeetCode, provide an interview simulator, run background AI, or offer public signup or cross-user sharing. See `docs/product/out-of-scope.md`.

## Project Layout

```
backend/    ASP.NET Core solution (Domain, Application, Infrastructure, Api) + tests
frontend/   React 19 + TypeScript + Vite app — a separate application, not a Clean Architecture layer
```

## Target stack

- React 19 + TypeScript + Vite
- ASP.NET Core 10 Controllers
- Lightweight Clean Architecture with feature-folder use cases; no MediatR or Minimal APIs
- EF Core 10 + Npgsql + PostgreSQL on Supabase
- Backend-mediated Supabase Auth and private Storage
- Provider-neutral `IAIProvider`

## Documentation

| Document | Purpose |
|---|---|
| `CONTEXT.md` | Ubiquitous language and glossary |
| `docs/product/brief.md` | Product purpose, principles, and MVP |
| `docs/product/out-of-scope.md` | Explicit MVP exclusions |
| `docs/domain/model.md` | Aggregates, entities, value objects, and invariants |
| `docs/domain/use-cases.md` | Primary user journeys |
| `docs/architecture/solution.md` | Layers, dependencies, and key flows |
| `docs/architecture/persistence.md` | PostgreSQL/EF Core mapping strategy |
| `docs/testing/strategy.md` | Test layers and CI gates |
| `docs/security/threat-model.md` | Assets, threats, controls, and security tests |
| `docs/deployment/strategy.md` | Deployment topology and platform requirements |
| `docs/roadmap.md` | Implementation phases |
| `docs/tbd.md` | Decisions that intentionally remain open |
| `docs/prompts/phase-0a-claude-kickoff.md` | First implementation prompt for Claude Code |
| `docs/adr/` | Accepted architectural decisions |

Coding agents must read `AGENTS.md`. `CLAUDE.md` contains the shared project constraints and automated architecture rules.
