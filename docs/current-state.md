# CommitAhead — Current State

This is the operational handoff for a new development session. It records only the current status,
priority, and document routing; detailed rules remain in their authoritative documents.

## Current implementation state

- Phases 0–5 are implemented as described in `docs/roadmap.md`. Their remaining cross-cutting E2E
  acceptance is consolidated in Phase 6b.
- Phase 6a, the persistent local production-like Docker runtime, is implemented and
  infrastructure-verified.
- The isolated Playwright project, Docker stack, fixtures, scripts, database reset, and external
  stub are implemented. The four approved journey specs are not yet written.
- Phase 6c, internet production deployment, has not started and is explicitly deferred.

## Current priority and verification boundary

Use and validate the application locally. When real Supabase credentials are available, complete
the manual acceptance checklist in `README.md`; a real Anthropic key is needed only for its explicit
AI-analysis check.

The Phase 6a verification proved Docker build/startup, API health, SPA serving, migrations/RLS,
bootstrap, persistent database and Data Protection volumes, restart/reset behaviour, and the
absence of automatic Supabase or Anthropic calls. It did **not** prove real magic-link
authentication or authenticated product journeys because placeholder external configuration was
used.

After local acceptance, the next implementation work is **Phase 6b journey 001 only**, and only
when explicitly requested. Implement the journeys incrementally; do not implement all four at
once. Do not create `/devalente-e2e` until all four journeys are implemented and stable.

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
