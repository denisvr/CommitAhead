# CommitAhead

CommitAhead is a private, invite-only web application for structured software-engineering interview preparation. It combines a ranked study queue with professional-profile, job-analysis, and interview evidence so the user can answer: **what should I study next, and why?**

## Status

Phase 0's security and architecture baseline is implemented and verified: solution skeleton, EF Core/Npgsql, backend-mediated magic-link/PKCE auth with closed (invite-only) login, secure-by-default authorization, CSRF, security headers, and a minimal authenticated screen — including one real call to the live Supabase Auth API.

Phase 1 (the ranked study queue — StudyItem, StudyReview, PriorityOverride, ScoringConfig) and Phase 2 (ProfessionalProfile, CVPresentation, and their curated selections) are implemented end to end — domain, use cases, EF mappings/migrations, owner-scoped RLS, API controllers, and frontend pages — and covered by domain, use-case, real-runtime-role integration, API, and MSW-backed frontend component tests.

Phase 3 (Evidence Sources — JobAnalysis, JobRequirement, JobGap, InterviewNote, and the secure pasted-text/PDF-upload flow) is implemented end to end: domain, use cases, EF mappings/migrations (including a composite foreign key enforcing that a JobGap's RequirementId belongs to the same JobAnalysis, as defense-in-depth alongside the in-memory invariant), owner-scoped RLS, API controllers, and frontend pages (JobAnalysis/InterviewNote list/create/detail, including the uploaded PDF's extracted text shown for verification). The upload endpoint (`POST /api/job-analyses/upload`) extracts text with PdfPig under strict limits — a 10-second extraction budget that is best-effort (PdfPig's own API is synchronous and uncancellable, so a slow parse can keep running on its own thread after the budget elapses; container memory/CPU limits are the real backstop, not an in-process guarantee) — and uploads to Supabase Storage via the current user's own forwarded JWT (ADR-0018, never a service-role key). Storage cleanup after a rejected upload or a JobAnalysis deletion is best-effort: on failure it logs the orphaned object's key (never the exception itself) for manual remediation, rather than blocking the request. **Live PDF upload against the real Supabase project additionally requires the private `job-postings` bucket and its RLS policies from `scripts/database/006_storage_job_postings.sql` to be provisioned first** (see "Setting Up the Real Supabase Project" below) — it is not live-ready until that one-time operator step has run.

Phase 4 (AI-assisted analysis) is implemented end to end: `AnalyzeJobAnalysis`/`AnalyzeCVPresentation`/`AnalyzeInterviewNote` call `IAIProvider` (real implementation: `AnthropicAIProvider`, Claude Haiku 4.5 — ADR-0019; `FakeAIProvider` everywhere in automated tests, per the zero-real-AI-calls-in-CI rule) and produce an `AnalysisDraft` with immutable proposed content (StructuredSuggestions, LinkProposals, StudyItemProposals) that a human must explicitly decide on — AI never writes to domain entities directly (ADR-0005). Durable per-owner idempotency and a Reserved→Completed/Failed `AIUsageRecord` lifecycle (ADR-0014) make retries safe; a per-owner daily/monthly USD budget and an hourly rate limit are enforced before every AI call. `GetAnalysisDraft` (`GET /api/analysis-drafts/{id}`), `ApplyAnalysisDraft` (exactly one Accepted/Rejected decision per proposal, one atomic accepted-effects transaction, EvidenceLink creation for accepted LinkProposals), and `DiscardAnalysisDraft` (explicit Pending → Discarded, including for a draft with zero proposals) round out the write side. The frontend's `AnalysisDraftReviewPage` shows every proposal's full immutable proposed content before any decision, lets the user finalise accepted payloads, exposes Apply and Discard, and renders Applied/Discarded drafts as a read-only audit view; the "Analyze" trigger on `JobAnalysisDetailPage` holds one idempotency key across a transport/5xx retry and recovers an already-pending draft (via its id, carried in the `DraftAlreadyPending` conflict) instead of dead-ending. CVPresentation/InterviewNote "Analyze" trigger buttons are the one explicitly deferred piece — the review page itself is already source-agnostic.

Phase 5 (CV Export, ADR-0020: PDF via QuestPDF) is implemented and tested end to end, backend and frontend. `ExportCVPresentationUseCase` (`GET /api/cv-presentations/{id}/export`) resolves a CVPresentation's selected/ordered ProfessionalProfile entries, applies its `IncludeEmail`/`IncludePhone`/`IncludeAddress` visibility flags, rejects export explicitly for an unsupported `TemplateKey` (only `"modern-one-page"` renders) or `IncludePhoto=true` (no photo upload/storage path exists anywhere in this codebase), formats `YearMonth` dates locale-aware, and renders the result via `QuestPdfCVExportRenderer` (`IExportRenderer`, Infrastructure) — one A4 template rendering every field the export projection carries, including nested Markdown bullet lists. The renderer counts its own rendered pages internally (via PdfPig) and returns that count alongside the PDF bytes; `PageLimit` is enforced as a hard cap by the use case comparing against that count, never by constraining QuestPDF's layout mid-render (Application has no PDF-library dependency at all — only Infrastructure does). Markdown fields (summary, entry descriptions) go through `RestrictedMarkdownParser` (Markdig-based), mirroring `RestrictedMarkdown.tsx`/`restrictedUrlTransform.ts`'s exact allowlist — no images, no raw HTML, links kept only for https/http/mailto. The frontend adds a "Download PDF" button to `CVPresentationDetailPage` that fetches the PDF as a `Blob` (openapi-fetch's `parseAs: 'blob'`, keeping the existing 401-refresh middleware intact) and triggers a synthetic-anchor download, with an inline message for a not-found presentation or one that's unsupported (page limit, template, or photo); the form's Template field is a disabled single-option control and "Include photo" can only be unchecked. Every layer — the Markdown parser, the use case's selection/visibility/template/photo/page-limit logic, the renderer's actual PDF output, the endpoint's HTTP round trip, and the frontend download flow — is covered by tests that assert on real parsed values or a real triggered download, never a snapshot. See ADR-0020 for QuestPDF's actual (source-available, not MIT) Community License terms.

None of Phase 0/1/2/3/4 is marked fully complete: each phase's E2E exit criterion (Playwright) hasn't been written yet, deferred until there is a real deployed environment to run it against — see `docs/roadmap.md` for the exact per-phase checklist.

NetArchTest rule 5 (repository and `IAIProvider` production implementations exist only in Infrastructure) is fully active now that Phase 4 declared `IAIProvider` and shipped `AnthropicAIProvider` as its real implementation. Development uses Supabase Auth (remote) for authentication; the application's own PostgreSQL stays entirely local via Docker (development never connects to or migrates the real Supabase Postgres — see "Setting up the real Supabase project" below for why, and for the steps whenever you're ready to point at it, e.g. at deployment). Live AI calls additionally require a real Anthropic API key — configuration key `AI:Providers:Anthropic:ApiKey` (environment-variable form: `AI__Providers__Anthropic__ApiKey`), read lazily so everything else works with none configured; the key is never in CI or the browser. See `docs/roadmap.md` for the full picture.

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
then applies `scripts/database/002_rls_users.sql` (RLS on `users`), `scripts/database/003_rls_phase1.sql`
(grants/RLS on the Phase 1 business tables), `scripts/database/004_rls_phase2.sql` (grants/RLS on
the Phase 2 ProfessionalProfile/CVPresentation tables), `scripts/database/005_rls_phase3.sql`
(grants/RLS on the Phase 3 JobAnalysis/InterviewNote tables), and `scripts/database/007_rls_phase4.sql`
(grants/RLS on the Phase 4 AnalysisDraft/AIUsageRecord tables) — all safe to re-run. When the real
Supabase project is created, the same SQL scripts are the template for setting it up (see
`backend/scripts/database/`) — only the connection host/credentials change.

`scripts/database/006_storage_job_postings.sql` is different in kind, not just number: it targets
the real Supabase project's own managed `storage` schema (bucket + RLS for uploaded job-posting
PDFs — ADR-0018), which doesn't exist in the local Docker Postgres at all. It is **not** run by
`setup-local-db.ps1` and has no local-dev equivalent — applying it is a one-time operator action
against the real project, covered in "Setting Up the Real Supabase Project" below.

## Setting Up the Real Supabase Project

**Not required for local development.** `Supabase:Url`/`Supabase:AnonKey` point at the real
project for Auth, while `ConnectionStrings:CommitAheadDb` stays on the local Docker Postgres —
auth and persistence are independent, and there's no need to develop against the real Postgres
before deployment (Phase 6). The steps below apply the same
`backend/scripts/database/001_roles.sql`-`005_rls_phase3.sql` used locally to the *real* Postgres,
plus the Storage-only `006_storage_job_postings.sql`, for whenever you're ready (e.g. first
deployment). Only you should run these — they need the project's real database password, which
this assistant never sees or handles, even if you offer to share it:

```bash
# 1. In the Supabase SQL editor, run 001_roles.sql with the ${...} placeholders replaced by real
#    passwords you generate (never reuse the local dev ones).
# 2. Apply the migration bundle using the migrator role from step 1:
COMMITAHEAD_MIGRATION_CONNECTION="Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=commitahead_migrator;Password=<real password>" \
  dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
# 3. In the Supabase SQL editor, run 002_rls_users.sql (needs the `users` table from step 2),
#    then 003_rls_phase1.sql, 004_rls_phase2.sql, and 005_rls_phase3.sql (each needs its own
#    tables from the same migration).
# 4. Seed each enabled user's row (use their real Supabase Auth UID and email):
#    INSERT INTO users (id, supabase_user_id, email, is_enabled, created_at_utc)
#    VALUES ('<uid>', '<uid>', '<email>', true, now());
# 5. Point the running API at the real database with the app role from step 1:
dotnet user-secrets set "ConnectionStrings:CommitAheadDb" \
  "Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=commitahead_app;Password=<real password>" \
  --project src/CommitAhead.Api
# 6. In the Supabase dashboard or SQL editor, run 006_storage_job_postings.sql — creates the
#    private `job-postings` bucket and its RLS policies (ADR-0018). Unlike steps 1-4, this targets
#    Supabase's own managed `storage` schema, not the migrated application tables, and needs no
#    migration to have run first.
```

Also in the Supabase dashboard: Authentication → URL Configuration → add your callback URL
(`http://localhost:5120/auth/callback` for local dev) to the redirect allow-list, and confirm
Authentication → Sign In / Providers → "Allow new users to sign up" stays off (ADR-0006).

## Production (Local Docker)

Phase 6 (ADR-0021) starts with a hosting-neutral local deployment — a production Docker image and
Compose stack you can build, run, and use extensively before any cloud platform is chosen.
Deliberately provider-neutral: no Fly.io/Railway/Azure-specific configuration anywhere in it.

```bash
cp backend/.env.production.example backend/.env.production   # then edit real values
backend/scripts/setup-production-db.ps1                      # roles -> migrations -> RLS, own Postgres on :5434
docker compose -f docker-compose.prod.yml --env-file backend/.env.production up -d --build
curl http://localhost:8080/api/health
```

`Dockerfile` (repo root) is a multi-stage build: a Node stage builds the frontend, a **pinned**
`.NET SDK 10.0.302` stage publishes the backend (which copies the frontend build into `wwwroot`,
the same `CommitAhead.Api.csproj` target `dotnet publish` always uses), and a minimal ASP.NET Core
runtime stage runs it as a non-root user, exposing `/api/health` as a Docker `HEALTHCHECK`. The SDK
stage must stay pinned to the exact version in `backend/global.json` — the floating `10.0` tag can
resolve to a later feature band that `global.json`'s default `rollForward` policy refuses to run,
failing the build with an SDK-not-found error (discovered by actually building the image).

`docker-compose.prod.yml` runs that image alongside a dedicated PostgreSQL, both with named volumes
and `restart: unless-stopped` — it sits alongside `backend/docker-compose.yml` (the dev-only
Postgres) without conflict, using different ports (5434 vs 5433) and volumes. `ASPNETCORE_ENVIRONMENT=Docker`
is this stack's own environment name: it skips `UseHsts()`/`UseHttpsRedirection()` (this stack has
no TLS termination of its own — a real deployment behind a reverse proxy would use `Production` and
keep both), and Data Protection keys persist to a named volume (`DataProtection:KeyRingPath`) so
cookie encryption — and existing sessions — survive a container restart. Neither change affects
auth/CSRF cookies: they already read `Secure=true` unconditionally, and browsers treat
`http://localhost` as a secure context regardless of scheme, so they are still sent to this stack at
`http://localhost:8080`.

Migrations against this stack's own Postgres use `dotnet ef database update` directly (via
`backend/scripts/setup-production-db.ps1`, mirroring `setup-local-db.ps1`), since the SDK is already
on the machine running that script. `backend/scripts/build-migration-bundle.ps1` produces a
self-contained EF migration bundle (`backend/artifacts/efbundle`, gitignored) for wherever that
assumption stops holding — a real deployment target without the .NET SDK installed.

**Still explicitly deferred**, per ADR-0021 — none of this is resolved by the local stack above:
hosting platform, secrets management, Data Protection key encryption at rest, automated encrypted
backups, and centralized log retention. See `docs/tbd.md` for the target policies already decided
(30-day log retention; 30-day backup retention with a quarterly restore test) and what's still open.

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
| `docs/design/design-system/readme.md` | Approved Reading Room/Bookmark identity and frontend design contract |
| `docs/roadmap.md` | Implementation phases |
| `docs/tbd.md` | Decisions that intentionally remain open |
| `docs/prompts/phase-0a-claude-kickoff.md` | First implementation prompt for Claude Code |
| `docs/adr/` | Accepted architectural decisions |

Coding agents must read `AGENTS.md`. `CLAUDE.md` contains the shared project constraints and automated architecture rules.
