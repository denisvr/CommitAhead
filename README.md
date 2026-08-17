# CommitAhead

CommitAhead is a private, invite-only web application for structured software-engineering interview preparation. It combines a ranked study queue with professional-profile, job-analysis, and interview evidence so the user can answer: **what should I study next, and why?**

## Status

Phase 0's security and architecture baseline is implemented and verified: solution skeleton, EF Core/Npgsql, backend-mediated magic-link/PKCE auth with closed (invite-only) login, secure-by-default authorization, CSRF, security headers, and a minimal authenticated screen — including one real call to the live Supabase Auth API.

Phase 1 (the ranked study queue — StudyItem, StudyReview, PriorityOverride, ScoringConfig) and Phase 2 (ProfessionalProfile, CVPresentation, and their curated selections) are implemented end to end — domain, use cases, EF mappings/migrations, owner-scoped RLS, API controllers, and frontend pages — and covered by domain, use-case, real-runtime-role integration, API, and MSW-backed frontend component tests.

Phase 3 (Evidence Sources — JobAnalysis, JobRequirement, JobGap, InterviewNote, and the secure pasted-text/PDF-upload flow) is implemented end to end: domain, use cases, EF mappings/migrations (including a composite foreign key enforcing that a JobGap's RequirementId belongs to the same JobAnalysis, as defense-in-depth alongside the in-memory invariant), owner-scoped RLS, API controllers, and frontend pages (JobAnalysis/InterviewNote list/create/detail, including the uploaded PDF's extracted text shown for verification). The upload endpoint (`POST /api/job-analyses/upload`) extracts text with PdfPig under strict limits — a 10-second extraction budget that is best-effort (PdfPig's own API is synchronous and uncancellable, so a slow parse can keep running on its own thread after the budget elapses; container memory/CPU limits are the real backstop, not an in-process guarantee) — and uploads to Supabase Storage via the current user's own forwarded JWT (ADR-0018, never a service-role key). Storage cleanup after a rejected upload or a JobAnalysis deletion is best-effort: on failure it logs the orphaned object's key (never the exception itself) for manual remediation, rather than blocking the request. **Live PDF upload against the real Supabase project additionally requires the private `job-postings` bucket and its RLS policies from `scripts/database/006_storage_job_postings.sql` to be provisioned first** (see "Setting Up the Real Supabase Project" below) — it is not live-ready until that one-time operator step has run.

Phase 4 (AI-assisted analysis) is implemented end to end: `AnalyzeJobAnalysis`/`AnalyzeCVPresentation`/`AnalyzeInterviewNote` call `IAIProvider` (real implementation: `AnthropicAIProvider`, Claude Haiku 4.5 — ADR-0019; `FakeAIProvider` everywhere in automated tests, per the zero-real-AI-calls-in-CI rule) and produce an `AnalysisDraft` with immutable proposed content (StructuredSuggestions, LinkProposals, StudyItemProposals) that a human must explicitly decide on — AI never writes to domain entities directly (ADR-0005). Durable per-owner idempotency and a Reserved→Completed/Failed `AIUsageRecord` lifecycle (ADR-0014) make retries safe; a per-owner daily/monthly USD budget and an hourly rate limit are enforced before every AI call. `GetAnalysisDraft` (`GET /api/analysis-drafts/{id}`), `ApplyAnalysisDraft` (exactly one Accepted/Rejected decision per proposal, one atomic accepted-effects transaction, EvidenceLink creation for accepted LinkProposals), and `DiscardAnalysisDraft` (explicit Pending → Discarded, including for a draft with zero proposals) round out the write side. The frontend's `AnalysisDraftReviewPage` shows every proposal's full immutable proposed content before any decision, lets the user finalise accepted payloads, exposes Apply and Discard, and renders Applied/Discarded drafts as a read-only audit view; the "Analyze" trigger on `JobAnalysisDetailPage` holds one idempotency key across a transport/5xx retry and recovers an already-pending draft (via its id, carried in the `DraftAlreadyPending` conflict) instead of dead-ending. CVPresentation/InterviewNote "Analyze" trigger buttons are the one explicitly deferred piece — the review page itself is already source-agnostic.

Phase 5 (CV Export, ADR-0020: PDF via QuestPDF) is implemented and tested end to end, backend and frontend. `ExportCVPresentationUseCase` (`GET /api/cv-presentations/{id}/export`) resolves a CVPresentation's selected/ordered ProfessionalProfile entries, applies its `IncludeEmail`/`IncludePhone`/`IncludeAddress` visibility flags, rejects export explicitly for an unsupported `TemplateKey` (only `"modern-one-page"` renders) or `IncludePhoto=true` (no photo upload/storage path exists anywhere in this codebase), formats `YearMonth` dates locale-aware, and renders the result via `QuestPdfCVExportRenderer` (`IExportRenderer`, Infrastructure) — one A4 template rendering every field the export projection carries, including nested Markdown bullet lists. The renderer counts its own rendered pages internally (via PdfPig) and returns that count alongside the PDF bytes; `PageLimit` is enforced as a hard cap by the use case comparing against that count, never by constraining QuestPDF's layout mid-render (Application has no PDF-library dependency at all — only Infrastructure does). Markdown fields (summary, entry descriptions) go through `RestrictedMarkdownParser` (Markdig-based), mirroring `RestrictedMarkdown.tsx`/`restrictedUrlTransform.ts`'s exact allowlist — no images, no raw HTML, links kept only for https/http/mailto. The frontend adds a "Download PDF" button to `CVPresentationDetailPage` that fetches the PDF as a `Blob` (openapi-fetch's `parseAs: 'blob'`, keeping the existing 401-refresh middleware intact) and triggers a synthetic-anchor download, with an inline message for a not-found presentation or one that's unsupported (page limit, template, or photo); the form's Template field is a disabled single-option control and "Include photo" can only be unchecked. Every layer — the Markdown parser, the use case's selection/visibility/template/photo/page-limit logic, the renderer's actual PDF output, the endpoint's HTTP round trip, and the frontend download flow — is covered by tests that assert on real parsed values or a real triggered download, never a snapshot. See ADR-0020 for QuestPDF's actual (source-available, not MIT) Community License terms.

Phase 6b's four Playwright journeys are now implemented and passing against the isolated local E2E stack (`docker-compose.e2e.yml`, `e2e/`) — each verified standalone and together via the guaranteed-teardown `npm run e2e:full`, with zero unexpected calls to the deterministic external stub. Journey 1 satisfies Phase 0's E2E exit criterion, Journey 2 satisfies Phase 1's, Journey 4 satisfies Phase 2's and Phase 5's, and Journey 3 satisfies Phase 4's — Phases 0, 1, 2, 4, and 5 are now all fully complete (Phase 3 never had an explicit E2E gate). Building Journey 3 also surfaced and fixed a real, previously-undetected casing defect in `AnthropicStructuredOutputSchema` — see `docs/testing/strategy.md` Layer 7 and `docs/roadmap.md` Phase 4/6b for the full account. Phase 5's post-merge visual-regression fixture (`QuestPdfCVExportRendererVisualRegressionTests`, a tolerant per-pixel diff against one committed baseline PNG per template) is also now implemented, making Phase 5 fully complete. Phase 6c (internet deployment, explicitly deferred pending a hosting decision) is the only open work remaining — see `docs/roadmap.md` for the exact checklist.

NetArchTest rule 5 (repository and `IAIProvider` production implementations exist only in Infrastructure) is fully active now that Phase 4 declared `IAIProvider` and shipped `AnthropicAIProvider` as its real implementation. Development uses a fully local Supabase instance for authentication (ADR-0023, via `supabase start` — see "Local Supabase (Development)" below), and the application's own PostgreSQL is also entirely local via Docker; neither ever touches Supabase Cloud during development (see "Setting Up Supabase Cloud (Production Only)" below for when that changes, at deployment). Live AI calls additionally require a real Anthropic API key — configuration key `AI:Providers:Anthropic:ApiKey` (environment-variable form: `AI__Providers__Anthropic__ApiKey`), read lazily so everything else works with none configured; the key is never in CI or the browser. See `docs/roadmap.md` for the full picture.

## Local Requirements

- .NET SDK `10.0.302` (pinned in `backend/global.json`)
- Node.js `24` (pinned in `frontend/.nvmrc`)
- Docker, for the local development database (see below)

```bash
cd backend && dotnet build && dotnet test
cd frontend && npm ci && npm run lint && npm test && npm run build
```

## Local Database (Development)

Development uses a fully local Supabase instance for authentication (see "Local Supabase
(Development)" below, ADR-0023) — the application's own PostgreSQL, set up here, is a completely
separate local Docker Postgres, never Supabase's (local or Cloud). Neither ever touches Supabase
Cloud during development (see "Setting Up Supabase Cloud (Production Only)" below for when that
changes, at deployment).

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

## Local Supabase (Development)

CommitAhead uses Supabase Auth as its only identity provider (ADR-0006), backend-mediated — but
during development, that Supabase instance is **entirely local** (ADR-0023), via the official
[Supabase CLI](https://supabase.com/docs/guides/local-development/cli/getting-started). No
Supabase project, credentials, or internet connection required. Supabase Cloud is reserved for
production only (Phase 6c, still deferred).

```bash
npx supabase@latest start   # first run pulls several Docker images — a few minutes
```

This prints (and `npx supabase@latest status -o json` reprints later) the local instance's
`API_URL` (`http://127.0.0.1:54321`), `ANON_KEY` (a fixed, published local demo key — not a
secret), `SERVICE_ROLE_KEY`, and `MAILPIT_URL` (`http://127.0.0.1:54324` — every magic-link email
sent locally lands here instead of a real inbox, no configuration needed). Point the backend at it:

```bash
cd backend
dotnet user-secrets set "Supabase:Url" "http://127.0.0.1:54321" --project src/CommitAhead.Api
dotnet user-secrets set "Supabase:AnonKey" "<ANON_KEY from supabase status>" --project src/CommitAhead.Api
```

A fresh local Supabase instance has no users at all, and closed login (ADR-0015) rejects every
email until one exists locally too — `backend/scripts/bootstrap-local-supabase-user.ps1` creates
the Supabase Auth user (idempotent — finds it if it already exists) and seeds/enables the matching
row in the local Postgres `users` table in one step:

```bash
powershell -File scripts/bootstrap-local-supabase-user.ps1 -Email "you@example.com"
```

Then `dotnet run --project src/CommitAhead.Api`, `POST /auth/login` with that email, and open
Mailpit (`http://127.0.0.1:54324`) to click the real magic link — the full magic-link → session →
refresh → logout cycle works exactly like it does against Supabase Cloud, just without leaving
your machine.

**For `docker-compose.dev.yml` (below):** the `api` container reaches the local Supabase instance
via `host.docker.internal`, not `127.0.0.1` — that address means the container itself there, not
the host running `supabase start`'s own containers:

```
SUPABASE_URL=http://host.docker.internal:54321
SUPABASE_ANON_KEY=<ANON_KEY from supabase status>
```

in `backend/.env` (already read by `docker-compose.dev.yml`'s `api` service).

See ADR-0023 for the two `AuthenticationServiceCollectionExtensions.cs` fixes this needed
(`RequireHttpsMetadata`, and refetching JWKS from the caller's own reachable address instead of
GoTrue's self-reported `jwks_uri`) — neither is a local-only special case; both are strictly more
correct treatments of the OIDC discovery contract that happen to matter for Cloud not at all today.

`supabase stop` shuts the local instance down; `supabase stop --no-backup` also discards its data
(the Supabase CLI's own internal Postgres, on port 54322 — entirely separate from the application's
own local Postgres above, never shared).

## Local Development (Fully Containerized, Hot-Reload)

An alternative to the workflow above (ADR-0022) for when you'd rather not install the .NET SDK or
Node.js locally at all — everything, including migrations, runs in containers, and editing code
reflects immediately (`dotnet watch` / Vite's dev server), no rebuild needed:

```bash
cp backend/.env.example backend/.env   # then edit real values — Supabase/Anthropic are optional
docker compose -f backend/docker-compose.yml -f docker-compose.dev.yml up -d --build
```

Then open <http://localhost:5173> — the frontend dev server, proxying `/api`/`/auth` to the `api`
container internally. The API is also directly reachable at <http://localhost:5120> (same port as
a host `dotnet run`), and `/api/health` should return `200` once `db-init` finishes.

**Always list `backend/docker-compose.yml` first** in `-f` — this stack layers `db-init`/`api`/
`frontend` onto that file's existing `db` service rather than duplicating it, and Compose derives
its default project name from the first `-f` file's directory. Keeping that order means this
workflow and "run the API/frontend directly on the host" (above) resolve to the same project name
and therefore share the exact same database/volume — you can freely switch between the two without
switching data.

`backend/scripts/db-init/` builds and runs a self-contained EF migration bundle inside its own
image on first `up` — the same approach `e2e/support/db-init/` already uses, deliberately a
separate copy (see that Dockerfile's own header, and ADR-0022, for why) so this dev stack and the
isolated E2E stack stay genuinely separate environments. This is what makes the whole workflow
need zero host .NET SDK or Node.js: `db-init`, `api`, and `frontend` each build/run entirely inside
their own images.

Logs, shutdown, and reset follow the same shape as every other Compose stack in this repo:

```bash
# Logs (all three new services, following)
docker compose -f backend/docker-compose.yml -f docker-compose.dev.yml logs -f db-init api frontend

# Shut down — containers stop, the db volume and NuGet/node_modules caches are kept
docker compose -f backend/docker-compose.yml -f docker-compose.dev.yml down

# Clean reset — discards the database too (NuGet/node_modules caches survive; re-run `up` to
# re-migrate from scratch)
docker compose -f backend/docker-compose.yml -f docker-compose.dev.yml down -v
```

Session cookies and Data Protection keys are **not** persisted across the `api` container being
recreated (no `DataProtection:KeyRingPath` here, matching plain `Development` behavior) — restarting
`api` signs everyone out, exactly like restarting a host `dotnet run` process. Not something this
workflow needed to fix; not a regression it introduced either.

## Setting Up Supabase Cloud (Production Only)

**Not required for local development** — see "Local Supabase (Development)" above (ADR-0023).
`Supabase:Url`/`Supabase:AnonKey` point at the real Cloud project for Auth, while
`ConnectionStrings:CommitAheadDb` stays on the local Docker Postgres — auth and persistence are
independent, and there's no need to develop against the real Postgres before internet deployment
(Phase 6c). The steps below apply the same
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

Also in the Supabase dashboard: Authentication → URL Configuration → add **both** callback URLs
this project actually uses — `http://localhost:5120/auth/callback` (local `dotnet run`) and
`http://localhost:8080/auth/callback` (the local Docker stack below, ADR-0021) — to the redirect
allow-list, and confirm Authentication → Sign In / Providers → "Allow new users to sign up" stays
off (ADR-0006). Each backend environment sends its own URL as a percent-encoded `redirect_to` query
parameter on the `/auth/v1/otp` call (`Auth:CallbackUrl` configuration — `appsettings.Development.json`
for local dev, `Auth__CallbackUrl` in `docker-compose.prod.yml` for Docker) — GoTrue reads
`redirect_to` only from the query string, never from the JSON body, so it must go there, not
alongside the OTP/PKCE payload. It is trusted backend configuration only, never derived from a
request's Origin/Referer.

## Production (Local Docker)

Phase 6 (ADR-0021) starts with a hosting-neutral local deployment — a production Docker image and
Compose stack you can build, run, and use extensively before any cloud platform is chosen.
Deliberately provider-neutral: no Fly.io/Railway/Azure-specific configuration anywhere in it.

```bash
cp backend/.env.production.example backend/.env.production   # then edit real values — see below, "change_me" is rejected
backend/scripts/setup-production-db.ps1                      # roles -> migrations -> RLS, own Postgres on 127.0.0.1:5434
backend/scripts/bootstrap-production-user.ps1                # seeds the one enabled User row closed login needs
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
Postgres) without conflict, using different ports (5434 vs 5433) and volumes. Both the app's `8080`
and the db's `5434` are bound to `127.0.0.1` only, not `0.0.0.0` — this stack is for local use, not
for being reachable from the rest of the LAN. The `app` service also carries a `deploy.resources.limits`
(1 CPU / 1 GiB) — the real backstop against a runaway, uncancellable PDF parse
(`docs/security/threat-model.md`, "PDF Upload") is the container's own resource ceiling, not
anything in-process, so this makes that claim actually true for this stack rather than aspirational.
`ASPNETCORE_ENVIRONMENT=Docker` is this stack's own environment name: it skips
`UseHsts()`/`UseHttpsRedirection()` (this stack has no TLS termination of its own — a real
deployment behind a reverse proxy would use `Production` and keep both), and Data Protection keys
persist to a named volume (`DataProtection:KeyRingPath`) so cookie encryption — and existing
sessions — survive a container restart. Neither change affects auth/CSRF cookies: they already read
`Secure=true` unconditionally, and browsers treat `http://localhost` as a secure context regardless
of scheme, so they are still sent to this stack at `http://localhost:8080`.

A freshly-migrated database has no `User` row at all, and closed login (ADR-0015) rejects every
email until one exists — `backend/scripts/bootstrap-production-user.ps1` inserts/updates exactly
one row in the local PostgreSQL `users` table (never a Supabase account, never public signup),
reading `INITIAL_USER_ID`/`INITIAL_USER_EMAIL` from `backend/.env.production` (or `-UserId`/`-Email`
parameters); the Supabase Auth user with that id must already exist in the real project (see
"Setting Up the Real Supabase Project" above). `setup-production-db.ps1` also now rejects the
`change_me` placeholder `.env.production.example` ships for every credential, rather than silently
bootstrapping a "production-like" database on values nobody actually set.

Migrations against this stack's own Postgres use `dotnet ef database update` directly (via
`backend/scripts/setup-production-db.ps1`, mirroring `setup-local-db.ps1`), since the SDK is already
on the machine running that script. `backend/scripts/build-migration-bundle.ps1` produces a
self-contained EF migration bundle (`backend/artifacts/efbundle`, gitignored) for wherever that
assumption stops holding — a real deployment target without the .NET SDK installed.

Since this stack is meant to be used extensively (and its `professional_profiles`/etc. content
routinely includes non-ASCII text, e.g. Portuguese), `backend/scripts/backup-production-db.ps1` /
`restore-production-db.ps1` give it a real, lossless manual backup command — `pg_dump --format=custom`
run entirely inside the `db` container, copied out as a raw binary file with `docker compose cp`
to a timestamped `backend/backups/*.dump` (gitignored). Never passes the dump's bytes through
PowerShell's text pipeline/encoding, so it round-trips accented characters exactly. Restoring
(`pg_restore --single-transaction --exit-on-error --clean --if-exists`, also run inside the
container) is an atomic all-or-nothing operation — any error rolls the whole restore back rather
than leaving the database half-restored — and intentionally preserves ownership/grants exactly as
dumped, reassigning tables back to `commitahead_migrator` (required for future EF migrations) and
grants back to `commitahead_app` (required for the running app), not just the row data; RLS
policies come back automatically as ordinary table metadata, no separate re-apply step. The script
detects whether `app` was actually running *before* touching anything, stops it for the duration of
the restore, and restarts it afterward **only if** the restore succeeded, the post-restore
`commitahead_app` connection check succeeded, **and** it was running beforehand — otherwise the app
is left stopped and the script exits non-zero, rather than pointing a running app at a database
that failed to restore correctly. The temporary dump file inside the container is always removed in
cleanup, whether the restore succeeded or not, without masking whichever failure actually occurred.
Not automated, not encrypted, not on any retention schedule — that's still the deferred
cloud-deployment work below.

**Everyday operations:**

```bash
# Logs (both services, following)
docker compose -f docker-compose.prod.yml --env-file backend/.env.production logs -f

# Shut down — containers stop, named volumes (db data, Data Protection keys) are kept
docker compose -f docker-compose.prod.yml --env-file backend/.env.production down

# Start again later — same volumes, same data, no rebuild needed
docker compose -f docker-compose.prod.yml --env-file backend/.env.production up -d

# Clean reset — discards ALL local data (db + Data Protection keys) and starts from empty.
# Re-run setup-production-db.ps1 and bootstrap-production-user.ps1 afterward.
docker compose -f docker-compose.prod.yml --env-file backend/.env.production down -v
```

Always pass `--env-file backend/.env.production` to every `docker compose ... -f docker-compose.prod.yml`
command, including `logs`/`ps`/`down` — omitting it makes Compose evaluate every `${VAR}` in the
file against an empty environment instead, which does not affect already-running containers but
prints a `variable is not set` warning per undefined one.

Empirically verified (this exact command sequence, end to end, against a disposable local
`backend/.env.production`): `setup-production-db.ps1` → `bootstrap-production-user.ps1` → `up -d
--build` → `/api/health` returns `200 {"status":"Healthy"}` → the built SPA is served at `/` →
`down` (no `-v`) → `up -d` again with no rebuild → the seeded `users` row and `/api/health` are
both still there, proving the named volumes actually persist data across a routine restart. The
app started and served every check above with a placeholder Supabase URL and a blank
`ANTHROPIC_API_KEY` — both are validated lazily, per request, never at startup — which is also the
proof that **no automatic Supabase or Anthropic call ever happens**: Anthropic is only ever called
when you explicitly trigger an "Analyze" action from a signed-in session, and Supabase is only ever
called on an actual login/refresh/logout attempt. **What this did not prove**: real Supabase
magic-link login/logout, or any authenticated end-user journey — placeholder external configuration
was used throughout, deliberately, so none of that was exercised. See "Manual acceptance checklist"
directly below for what to verify once you configure a real Supabase project (and, for the AI
checks, a real Anthropic key).

**Manual acceptance checklist (needs real credentials):** the infrastructure verification above
proves the container/database/persistence mechanics, nothing about the actual product working for
a real signed-in user. Once `backend/.env.production` has a real `SUPABASE_URL`/`SUPABASE_ANON_KEY`
(and, to check AI analysis, a real `ANTHROPIC_API_KEY`), click through this by hand — none of it is
automated, and it is not part of CI:

- [ ] Magic-link login completes and logout clears the session
- [ ] StudyItem create → review → ranked-queue ordering works end to end
- [ ] Professional profile → CV presentation flow (create, edit selections, preview) works end to end
- [ ] Job posting flow — pasted text and PDF upload — extracts and saves correctly (PDF upload additionally needs the private Supabase Storage bucket/RLS policies from `scripts/database/006_storage_job_postings.sql` provisioned first)
- [ ] An explicit AI analysis (Analyze → review the draft → apply) completes against the real Anthropic key
- [ ] Restart the stack (`down` then `up -d`, no `-v`) and confirm data persists and session/login behaves as expected (still signed in via cookie, or a fresh login is required — whichever matches the app's actual session lifetime, not assumed)

**Still explicitly deferred**, per ADR-0021 — none of this is resolved by the local stack above:
hosting platform, secrets management, Data Protection key encryption at rest, automated/encrypted
backups on a retention schedule, and centralized log retention. See `docs/tbd.md` for the target
policies already decided (30-day log retention; 30-day backup retention with a quarterly restore
test) and what's still open.

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
- EF Core 10 + Npgsql; PostgreSQL in Docker locally, with Supabase PostgreSQL deferred to Phase 6c
- Backend-mediated Supabase Auth and private Storage
- Provider-neutral `IAIProvider`

## Documentation

| Document | Purpose |
|---|---|
| `docs/current-state.md` | Current implementation state, priority, deferrals, and handoff for a new session |
| `CONTEXT.md` | Ubiquitous language and glossary |
| `docs/product/brief.md` | Product purpose, principles, and MVP |
| `docs/product/out-of-scope.md` | Explicit MVP exclusions |
| `docs/domain/model.md` | Aggregates, entities, value objects, and invariants |
| `docs/domain/use-cases.md` | Primary user journeys |
| `docs/architecture/solution.md` | Layers, dependencies, and key flows |
| `docs/architecture/persistence.md` | PostgreSQL/EF Core mapping strategy |
| `docs/testing/strategy.md` | Test layers and CI gates; Layer 7 is the normative Playwright E2E contract |
| `e2e/README.md` | E2E operational runbook (install, Docker stack, running journeys, artifacts, safeguards) |
| `docs/security/threat-model.md` | Assets, threats, controls, and security tests |
| `docs/deployment/strategy.md` | Deployment topology and platform requirements |
| `docs/design/design-system/readme.md` | Approved Reading Room/Bookmark identity and frontend design contract |
| `docs/roadmap.md` | Implementation phases |
| `docs/tbd.md` | Decisions that intentionally remain open |
| `docs/prompts/phase-0a-claude-kickoff.md` | First implementation prompt for Claude Code |
| `docs/adr/` | Accepted architectural decisions |

Coding agents must read `AGENTS.md`. `CLAUDE.md` contains the shared project constraints and automated architecture rules.
