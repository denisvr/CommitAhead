# CommitAhead

CommitAhead is a private, invite-only web application for structured software-engineering interview preparation. It combines a ranked study queue with professional-profile, job-analysis, and interview evidence so the user can answer: **what should I study next, and why?**

## Status

Phase 0's security and architecture baseline is implemented and verified: solution skeleton, EF Core/Npgsql, backend-mediated magic-link/PKCE auth with closed (invite-only) login, secure-by-default authorization, CSRF, security headers, and a minimal authenticated screen — including one real call to the live Supabase Auth API. Two pieces remain pending, not complete: the E2E (Playwright) project has not been added yet, and the `IAIProvider` half of NetArchTest rule 5 stays skipped until Phase 4 declares that interface. There is no business domain layer yet (Phase 1). Development uses Supabase Auth (remote) for authentication; the application's own PostgreSQL stays entirely local via Docker (development never connects to or migrates the real Supabase Postgres — see "Setting up the real Supabase project" below for why, and for the steps whenever you're ready to point at it, e.g. at deployment). See `docs/roadmap.md` for the full picture.

## Local Requirements

- .NET SDK `10.0.302` (pinned in `backend/global.json`)
- Node.js `24` (pinned in `frontend/.nvmrc`)
- Docker, for the local development database (see below)

```bash
cd backend && dotnet build && dotnet test
cd frontend && npm ci && npm run lint && npm test && npm run build
```

## Local Database (Development)

Development uses Supabase Auth (remote) for authentication, but the application's own PostgreSQL
stays entirely local via Docker — the real Supabase Postgres is never connected to or migrated
during development (see "Setting Up the Real Supabase Project" below for when that changes, at
deployment).

```bash
cd backend
cp .env.example .env               # then edit the passwords
dotnet user-secrets set "ConnectionStrings:CommitAheadDb" \
  "Host=localhost;Port=5433;Database=commitahead;Username=commitahead_app;Password=<COMMITAHEAD_APP_PASSWORD from .env>" \
  --project src/CommitAhead.Api
powershell -File scripts/setup-local-db.ps1
```

`scripts/setup-local-db.ps1` is the single reproducible entry point for roles → migrations → RLS —
see `docs/architecture/persistence.md` ("Migration Strategy") for why that split exists and which
artifact is authoritative for each. It: starts the Postgres container (`docker compose up`, which
runs `scripts/database/001_roles.sql` automatically on first start, creating the
`commitahead_app`/`commitahead_migrator` roles from `.env`), applies pending EF Core migrations,
then applies `scripts/database/002_rls_users.sql` (RLS on `users`) — safe to re-run. When the real
Supabase project is created, the same two SQL scripts are the template for setting it up (see
`backend/scripts/database/`) — only the connection host/credentials change.

## Setting Up the Real Supabase Project

**Not required for local development.** `Supabase:Url`/`Supabase:AnonKey` point at the real
project for Auth, while `ConnectionStrings:CommitAheadDb` stays on the local Docker Postgres —
auth and persistence are independent, and there's no need to develop against the real Postgres
before deployment (Phase 6). The steps below apply the same
`backend/scripts/database/001_roles.sql`/`002_rls_users.sql` used locally to the *real* Postgres,
for whenever you're ready (e.g. first deployment). Only you should run these — they need the
project's real database password, which this assistant never sees or handles, even if you offer
to share it:

```bash
# 1. In the Supabase SQL editor, run 001_roles.sql with the ${...} placeholders replaced by real
#    passwords you generate (never reuse the local dev ones).
# 2. Apply the migration bundle using the migrator role from step 1:
COMMITAHEAD_MIGRATION_CONNECTION="Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=commitahead_migrator;Password=<real password>" \
  dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
# 3. In the Supabase SQL editor, run 002_rls_users.sql (needs the `users` table from step 2).
# 4. Seed each enabled user's row (use their real Supabase Auth UID and email):
#    INSERT INTO users (id, supabase_user_id, email, is_enabled, created_at_utc)
#    VALUES ('<uid>', '<uid>', '<email>', true, now());
# 5. Point the running API at the real database with the app role from step 1:
dotnet user-secrets set "ConnectionStrings:CommitAheadDb" \
  "Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=commitahead_app;Password=<real password>" \
  --project src/CommitAhead.Api
```

Also in the Supabase dashboard: Authentication → URL Configuration → add your callback URL
(`http://localhost:5120/auth/callback` for local dev) to the redirect allow-list, and confirm
Authentication → Sign In / Providers → "Allow new users to sign up" stays off (ADR-0006).

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
| `docs/design/visual-identity.md` | Proposed visual identity (color, type, layout) — not yet implemented in `frontend/` |
| `docs/roadmap.md` | Implementation phases |
| `docs/tbd.md` | Decisions that intentionally remain open |
| `docs/prompts/phase-0a-claude-kickoff.md` | First implementation prompt for Claude Code |
| `docs/adr/` | Accepted architectural decisions |

Coding agents must read `AGENTS.md`. `CLAUDE.md` contains the shared project constraints and automated architecture rules.
