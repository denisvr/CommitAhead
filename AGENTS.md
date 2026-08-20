# CommitAhead agent instructions

Before changing code or documentation:

1. Read `docs/current-state.md`, `README.md`, `CONTEXT.md`, and `CLAUDE.md`.
2. Read the relevant product, domain, architecture, testing, and security documents under `docs/`.
3. Read every ADR that affects the requested change.
4. Check `docs/tbd.md`; never resolve an open decision by assumption.

Before planning new work, confirm the current priority and explicit deferrals in
`docs/current-state.md`. Do not infer that a later roadmap phase has started.

The hard constraints and architecture rules in `CLAUDE.md` apply to every coding agent, including Codex. Do not introduce MediatR, Minimal APIs, generic use-case dispatchers, direct frontend access to Supabase/AI, persisted EffectiveScore, or real AI calls in automated CI.

Before changing frontend code, also read `docs/design/design-system/readme.md`,
`docs/design/design-system/components.md`, and
`docs/design/design-system/page-patterns.md`. Studio with the Bookmark mark is the only approved
visual direction (ADR-0024, superseding Reading Room). Every screen must work in light, in dark,
and with no explicit theme choice under a dark system preference. Design reference HTML is not production code and must never be copied
into `frontend/`.

If an implementation requires changing an accepted architectural decision, update or supersede the relevant ADR explicitly before changing code. Keep the roadmap and affected tests/security documentation consistent with the new decision.
