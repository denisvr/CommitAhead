# CommitAhead — Current State

This is the operational handoff for a new development session. It records only the current status,
priority, and document routing; detailed rules remain in their authoritative documents.

## Current implementation state

- Phases 0–5 are implemented as described in `docs/roadmap.md`. Their remaining cross-cutting E2E
  acceptance is consolidated in Phase 6b.
- Phase 6a, the persistent local production-like Docker runtime, is implemented and
  infrastructure-verified.
- The isolated Playwright project, Docker stack, fixtures, scripts, database reset, and external
  stub are implemented. All four approved journeys are implemented and passing — 1
  (`001-authenticated-access.spec.ts`), 2 (`002-study-queue-ranking.spec.ts`), 3
  (`003-job-analysis-draft.spec.ts`), and 4 (`004-cv-presentation-export.spec.ts`) — each verified
  standalone and together via the guaranteed-teardown `npm run e2e:full`, with zero unexpected
  external-stub requests and the stack fully removed afterward each time. Journey 3 also surfaced
  and fixed a real production defect in `AnthropicStructuredOutputSchema` (StructuredSuggestion
  payload and StudyItemProposal details fields were declared camelCase in the schema sent to the
  real Anthropic API, but every actual consumer has always required the canonical PascalCase those
  opaque JSON strings use everywhere else) — corrected with backend regression tests, not by
  changing the E2E stub's casing to dodge it. Journey 4 seeds a ProfessionalProfile/Experience
  entry via API (legitimate setup, not the tested behavior, per Layer 7 §7.9) and drives
  presentation creation, selection editing, and export/download entirely through the UI.
  The `/devalente-e2e` skill has not been created yet — it may now be planned as a separate,
  explicit next step.
- Phase 6c, internet production deployment, has not started and is explicitly deferred.
- A fully-containerized, hot-reload dev environment (ADR-0022) is implemented:
  `docker-compose.dev.yml` layers `db-init`/`api`/`frontend` onto `backend/docker-compose.yml`'s
  existing `db`, so it shares the same database/volume as the host-run dev workflow rather than
  forking a second one. No host .NET SDK or Node.js is required — `db-init`
  (`backend/scripts/db-init/`, a deliberately separate copy of the E2E stack's own migration-bundle
  approach, not a shared file) runs roles→migrations→RLS once per `up`, `api` runs `dotnet watch`,
  and `frontend` runs Vite's dev server, both with source bind-mounted for hot-reload.
- Fixed: an unconfigured `Supabase:Url` used to throw an unhandled `UriFormatException` (from
  `HttpClient.BaseAddress = new Uri(...)` at DI-construction time) on the first real request that
  resolved `ISupabaseAuthClient`/`IJobPostingStorage` — surfaced only once someone actually opened
  the SPA against a genuinely-empty `Supabase:Url` via the new dev container. Now the typed
  `HttpClient`s leave `BaseAddress` unset instead of throwing, and `RefreshUseCase`/`CallbackUseCase`
  catch the resulting call-time failure the same way `LoginUseCase`/`LogoutUseCase` already did,
  degrading to `AuthResult.Denied()` (403) instead of an unhandled 500.
- Development now uses a fully local Supabase instance for authentication (ADR-0023, via
  `supabase start` — no Supabase Cloud project, credentials, or internet connection needed
  during development; Cloud is reserved for production only, Phase 6c). A real magic-link login →
  session → refresh → logout cycle is now genuinely proven — via curl and via a real browser
  against a real (if local) Supabase Auth instance, Mailpit capturing the email, both from a
  host-run backend and from `docker-compose.dev.yml`'s containerized one. This surfaced and fixed
  two real bugs in `AuthenticationServiceCollectionExtensions.cs`, neither previously exercised
  because `Authority` had only ever pointed at a real Cloud project before: `RequireHttpsMetadata`
  (JwtBearer refuses a plain-HTTP Authority by default) and `LocalSupabaseOpenIdConfigurationRetriever`
  (GoTrue's discovery document always advertises `jwks_uri` as its own fixed, self-referential URL,
  unreachable from inside the `api` container, which reaches the same local instance via
  `host.docker.internal` — now refetches signing keys from the caller's own reachable address
  instead). `backend/scripts/bootstrap-local-supabase-user.ps1` (new) creates a local Supabase Auth
  user and seeds the matching local `users` row in one idempotent step.

## Current priority and verification boundary

Real magic-link authentication is now proven end to end against a local Supabase instance (see
above, ADR-0023) — this closes what used to be the Phase 6a manual acceptance checklist's one
unproven gap for the *local* case. The Phase 6a manual acceptance checklist itself
(`README.md`, against `docker-compose.prod.yml`) remains not yet run — it still uses placeholder
external configuration and has not been pointed at either the local Supabase instance or a real
Cloud project. This is a deferral, not a gap: nothing about it blocks Phase 6b, which runs against
its own isolated, credential-free E2E stack.

The Phase 6a infrastructure verification (already completed earlier) proved Docker build/startup,
API health, SPA serving, migrations/RLS, bootstrap, persistent database and Data Protection
volumes, restart/reset behaviour, and the absence of automatic Supabase or Anthropic calls, using
placeholder external configuration throughout — real magic-link login against *that* stack
specifically (`docker-compose.prod.yml`) is what the deferred manual acceptance checklist would
still prove, whenever the user chooses to pick it up (it could now use the local Supabase instance
instead of a real Cloud project, per ADR-0023's "Consequences").

Phase 6b is complete — all four approved journeys are implemented and passing. There is no next
Phase 6b journey. `/devalente-e2e` may be planned as a separate, explicit next step, but was
deliberately not created in the same change as journey 4 — plan it only when explicitly requested.

Do not begin Phase 6c, choose hosting, or implement internet-deployment controls without explicit
user authorization.

## Environment boundaries

| Environment | Purpose | Persistence and external services |
|---|---|---|
| Normal development (host-run) | Fast feature iteration, IDE debugging | Local Docker PostgreSQL; local Supabase instance for Auth (ADR-0023, `supabase start`) — never Cloud during development |
| Normal development (containerized, ADR-0022) | Same as above, without host .NET SDK/Node.js; hot-reload | Same local Docker PostgreSQL/volume as host-run — shares data, not a separate environment; reaches the same local Supabase instance via `host.docker.internal` |
| Phase 6a local production-like | Exercise the deployable image locally as the user will normally run it | Persistent local Docker PostgreSQL and Data Protection volumes; configured real external services are request-driven only |
| Phase 6b E2E | Run the four approved Playwright journeys explicitly | Isolated, non-persistent Docker stack; local Supabase/Anthropic stub; no real external calls |
| Phase 6c internet production | Future internet deployment | Deferred; hosting and production controls remain undecided |

Never reuse the Phase 6a database for E2E and never treat the E2E stack as staging or production.

## Authoritative documents

- `docs/roadmap.md` owns implementation status and next work.
- `CLAUDE.md` owns engineering constraints; `AGENTS.md` owns agent startup instructions.
- `CONTEXT.md` owns domain terminology.
- `docs/testing/strategy.md` Layer 7 owns E2E rules; `e2e/README.md` owns E2E commands.
- `README.md` owns local development, local production-like runtime, and manual acceptance commands.
- `docs/adr/` owns accepted architectural decisions.
- `docs/tbd.md` owns unresolved decisions. Never resolve one by assumption.

If documents conflict, use the owner above and correct the stale reference before implementation.
