# CommitAhead — Current State

This is the operational handoff for a new development session. It records only the current status,
priority, and document routing; detailed rules remain in their authoritative documents.

## Current implementation state

- Phases 0–5 are implemented as described in `docs/roadmap.md`. Their remaining cross-cutting E2E
  acceptance is consolidated in Phase 6b.
- Phase 6a, the persistent local production-like Docker runtime, is implemented and
  infrastructure-verified.
- The isolated Playwright project, Docker stack, fixtures, scripts, database reset, and external
  stub are implemented. Journey 1 (`001-authenticated-access.spec.ts`) is implemented and passing —
  verified via `npm run verify:foundation`, standalone, and via the guaranteed-teardown
  `npm run e2e:full`, with zero unexpected external-stub requests and the stack fully removed
  afterward each time. Journeys 2–4 are not yet written.
- Phase 6c, internet production deployment, has not started and is explicitly deferred.

## Current priority and verification boundary

The Phase 6a real-credentials manual acceptance checklist (`README.md`) remains deferred by user
choice — the user has chosen not to configure real Supabase/Anthropic credentials or run the local
production-like Docker stack for now. This is a deferral, not a gap: nothing about it blocks Phase
6b, which runs against its own isolated, credential-free E2E stack.

The Phase 6a infrastructure verification (already completed earlier) proved Docker build/startup,
API health, SPA serving, migrations/RLS, bootstrap, persistent database and Data Protection
volumes, restart/reset behaviour, and the absence of automatic Supabase or Anthropic calls. It did
**not** prove real magic-link authentication or authenticated product journeys because placeholder
external configuration was used — that is exactly what the deferred manual acceptance checklist
would prove, whenever the user chooses to pick it up.

Phase 6b journey 1 is complete. The next implementation work is **Phase 6b journey 002 only**, and
only when explicitly requested. Implement the remaining journeys incrementally; do not implement
002–004 all at once. Do not create `/devalente-e2e` until all four journeys are implemented and
stable.

Do not begin Phase 6c, choose hosting, or implement internet-deployment controls without explicit
user authorization.

## Environment boundaries

| Environment | Purpose | Persistence and external services |
|---|---|---|
| Normal development | Fast feature iteration | Local Docker PostgreSQL; real Supabase Auth/Storage and Anthropic only when explicitly configured and used |
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
