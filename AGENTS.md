# CommitAhead agent instructions

Before changing code or documentation:

1. Read `README.md`, `CONTEXT.md`, and `CLAUDE.md`.
2. Read the relevant product, domain, architecture, testing, and security documents under `docs/`.
3. Read every ADR that affects the requested change.
4. Check `docs/tbd.md`; never resolve an open decision by assumption.

The hard constraints and architecture rules in `CLAUDE.md` apply to every coding agent, including Codex. Do not introduce MediatR, Minimal APIs, generic use-case dispatchers, direct frontend access to Supabase/AI, persisted EffectiveScore, or real AI calls in automated CI.

If an implementation requires changing an accepted architectural decision, update or supersede the relevant ADR explicitly before changing code. Keep the roadmap and affected tests/security documentation consistent with the new decision.
