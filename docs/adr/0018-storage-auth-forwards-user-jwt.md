---
status: accepted
date: 2026-08-07
---

# Supabase Storage authentication forwards the current user's JWT, not a service-role key

**Status: superseded — this feature was removed from the app (see docs/roadmap.md). Kept for historical record.**

## Context

The secure PDF-upload flow (ADR-0010) needs the backend to upload and, on rejection or deletion,
delete objects in a private Supabase Storage bucket. Two ways to authenticate those calls were
considered: the project's service-role key (a single, always-privileged secret that bypasses
Storage RLS entirely) or the current request's own Supabase-issued access token (the exact JWT
already validated to authenticate that request against our own API — see
`AuthenticationServiceCollectionExtensions`).

## Decision

Every Storage call (`SupabaseStorageClient`) carries the backend-only anon key in `apikey` (as
`SupabaseAuthClient` already does for GoTrue) and the *current user's own* access token in
`Authorization: Bearer` — read fresh per request via `ICurrentUserAccessToken`
(`CurrentUserAccessTokenAccessor`, backed by the same `commitahead_access` cookie the JWT bearer
middleware already validates) and set on each `HttpRequestMessage` individually, never as an
`HttpClient` default header. The service-role key is never used for these calls.

`backend/scripts/database/006_storage_job_postings.sql` grants `INSERT`/`SELECT`/`DELETE` (never
`UPDATE`, never public/anon access) to the `authenticated` Postgres role on `storage.objects`,
scoped to `bucket_id = 'job-postings'` **and** a path-prefix match against `auth.uid()`. That RLS
is what actually enforces per-owner isolation for these calls, in the same way `003_rls_phase1.sql`
onward enforce it for the application's own Postgres tables.

## Why

- **No new secret to provision.** Today only `Supabase:Url`/`AnonKey` exist (used for Auth). A
  service-role key would be a second, more dangerous credential — a leak or misuse compromises
  every user's Storage objects, not just one request's.
- **Reuses infrastructure that already exists and is already validated.** The token forwarded to
  Storage is the identical JWT the JWT-bearer middleware just verified (issuer, audience,
  signature, `iat`-based 15-minute effective lifetime) to authenticate the very request that
  triggered the Storage call — no separate token-minting or caching layer.
- **RLS-native isolation, matching the app's existing security model.** ADR-0015's per-user
  authorization and the Postgres RLS scripts already isolate every other resource by the acting
  user; a service-role key would instead make the *application code* the sole thing standing
  between one user's uploaded file and another's, with Storage RLS providing no backstop at all
  (a service-role connection bypasses RLS unconditionally).

## Consequences

- Building the bucket and its RLS policies is a one-time operator action against the real Supabase
  project (`006_storage_job_postings.sql`), deferred to deployment (Phase 6) — exactly like
  applying `001_roles.sql`-`005_rls_phase3.sql` to the real project's own Postgres already is. No
  automated test runs this script; it targets a schema (`storage.*`) that doesn't exist in the
  local Docker Postgres used for development and CI.
- `SupabaseStorageClientTests` (Infrastructure.Tests) prove per-call token isolation against a
  stubbed HTTP handler — two calls "by" two different fake users must each carry their own bearer
  token, never a client-shared default — but cannot prove the real Storage RLS policies themselves;
  that only happens once the real bucket/policies are provisioned in Phase 6.
- If a future feature needs the backend to act on Storage *outside* any specific user's request
  context (e.g. a scheduled orphan-cleanup job, explicitly deferred by ADR-0011), it will need its
  own decision — there is no "current user" token to forward from a non-request context, and this
  ADR does not cover that case.

## Considered alternatives

A service-role key, matching the simpler "one privileged backend credential does everything"
model many backend-mediated integrations use. Rejected: it is strictly more code (the client
itself is no simpler either way) for strictly less defense-in-depth, and introduces a new
high-privilege secret this project would otherwise never need to provision, rotate, or protect.
