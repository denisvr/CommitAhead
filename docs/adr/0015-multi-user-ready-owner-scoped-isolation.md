---
status: accepted
date: 2026-07-28
---

# Multi-user-ready OwnerUserId isolation; no hardcoded OWNER_USER_ID

## Context

The system was originally specified as strictly single-user: `docs/product/brief.md` named a sole user, and ADR-0006 authorized every request by comparing the JWT `sub` against a single `OWNER_USER_ID` value held in server configuration. That check has no notion of "which user owns this data" — it only recognises one identity and rejects every other one with 403.

Today there is still exactly one real user, and public signup remains disabled. But baking a single hardcoded identity into the auth check and into the product framing would force a rearchitecture (auth middleware, every query, every repository) the day a second invited user is added. The goal is to make isolation-by-owner the default shape now, while implementing nothing beyond what Phase 0A requires (no auth, no persistence, no domain yet).

## Decision

Every user-owned aggregate is scoped by an `OwnerUserId`, not checked against a single well-known constant:

1. Auth middleware validates the JWT as before (issuer, audience, signature, expiry against Supabase's JWKS), and additionally resolves `sub` to an existing, enabled application `User` record. An identity with no matching `User` record receives 403. There is no special-cased "the owner" identity — every enabled `User` is authorized for their own data.
2. Every repository/query that reads or writes user-owned data is scoped by the authenticated request's `OwnerUserId`. This is an application-layer responsibility now and will be reinforced by database constraints/RLS per user once persistence is implemented (Phase 1+).
3. Public signup stays disabled. New users are provisioned out-of-band — the exact mechanism is an open decision (see `docs/tbd.md`), but it is never a public self-registration flow.
4. Users do not share data and cannot see each other's resources. There is no cross-user sharing feature in MVP scope.

This ADR supersedes item 4 of ADR-0006 (the `sub == OWNER_USER_ID` check) and the "Sole User" framing in `docs/product/brief.md`. It does not change PKCE, cookie handling, token lifetimes, or any other part of ADR-0006.

## Consequences

- `OWNER_USER_ID` is removed as a concept; there is no such environment variable or protected configuration value to provision.
- Every future aggregate that is user-owned (StudyItem, ProfessionalProfile, JobAnalysis, InterviewNote, ScoringConfig, etc.) carries an `OwnerUserId` column from the start, once those aggregates are implemented — this ADR does not implement them.
- `docs/architecture/solution.md`, `docs/security/threat-model.md`, `docs/architecture/persistence.md`, and `docs/deployment/strategy.md` are updated to remove `OWNER_USER_ID` references and describe owner-scoped authorization instead.
- Nothing in Phase 0A changes as a result: there is no auth, persistence, or domain layer yet. This decision governs how those layers must be built when their phases begin.
- How invited users are actually provisioned (Supabase Admin API, manual `User` row insertion, an admin CLI) is a new open decision recorded in `docs/tbd.md` — not resolved here.

## Considered Alternatives

Keeping the single hardcoded `OWNER_USER_ID` check was rejected: it would require rewriting the auth middleware and every data-access path to introduce per-user scoping later, exactly the rearchitecture this decision avoids. Implementing full multi-tenant signup now was also rejected — there is no domain or auth layer yet to attach it to, and public signup is explicitly out of scope regardless of user count.
