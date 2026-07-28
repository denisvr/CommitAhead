# CommitAhead

CommitAhead is a private, invite-only web application for structured software-engineering interview preparation. It combines a ranked study queue with professional-profile, job-analysis, and interview evidence so the user can answer: **what should I study next, and why?**

## Status

**Phase 0A**, plus everything else in Phase 0 that doesn't need a real Supabase project (EF Core/Npgsql wiring, OpenAPI + generated TypeScript client, CI), is implemented. There is no Supabase project, authentication, or business domain layer yet. See `docs/roadmap.md` for what's left and `docs/prompts/phase-0a-claude-kickoff.md` for what Phase 0A specifically covered.

## Local Requirements

- .NET SDK `10.0.302` (pinned in `backend/global.json`)
- Node.js `24` (pinned in `frontend/.nvmrc`)
- Docker, for the local development database (see below)

```bash
cd backend && dotnet build && dotnet test
cd frontend && npm ci && npm run lint && npm test && npm run build
```

## Local Database (Development)

Until a real Supabase project exists, development uses a plain Postgres container — not a stand-in
for Supabase Auth/Storage, just persistence:

```bash
cd backend
cp .env.example .env               # then edit the passwords
docker compose up -d
dotnet user-secrets set "ConnectionStrings:CommitAheadDb" \
  "Host=localhost;Port=5433;Database=commitahead;Username=commitahead_app;Password=<COMMITAHEAD_APP_PASSWORD from .env>" \
  --project src/CommitAhead.Api
COMMITAHEAD_MIGRATION_CONNECTION="Host=localhost;Port=5433;Database=commitahead;Username=commitahead_migrator;Password=<COMMITAHEAD_MIGRATOR_PASSWORD from .env>" \
  dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
docker compose exec -T db psql -U postgres -d commitahead < scripts/database/002_rls_users.sql
```

`docker compose up` runs `scripts/database/001_roles.sql` automatically on first start (creating the
`commitahead_app`/`commitahead_migrator` roles from `.env`). `002_rls_users.sql` enables RLS on
`users` and must be run manually, after migrations, because it needs the table to already exist.
When the real Supabase project is created, the same two SQL scripts are the template for setting it
up (see `backend/scripts/database/`) — only the connection host/credentials change.

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
