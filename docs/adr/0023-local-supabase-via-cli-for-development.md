# ADR-0023: Local Supabase (via CLI) for development, Supabase Cloud only for production

## Context

Since Phase 0 (ADR-0006), CommitAhead has always used Supabase Auth as its only identity
provider, backend-mediated — the browser never talks to Supabase directly, only the .NET backend
does, and only the backend holds `Supabase:Url`/`Supabase:AnonKey`. Until now, "development"
meant pointing those two values at a real Supabase Cloud project, even for local, offline-feeling
work — the app's own PostgreSQL was always local (`backend/docker-compose.yml`), but
authentication itself required a live internet connection to Supabase Cloud, and a magic-link
login required actually receiving an email.

The user explicitly asked for zero Supabase Cloud dependency during development: `supabase start`
(the official Supabase CLI) runs a complete local Supabase stack via Docker — Auth (GoTrue),
PostgREST, Storage, Realtime, Studio, and a bundled Mailpit instance for capturing outgoing
emails — entirely offline, with no project to create and no credentials to request. Supabase
Cloud remains reserved for production only (Phase 6c, still deferred).

## Decision

- `supabase/config.toml` (checked in) configures the local instance: `additional_redirect_urls`
  lists both `http://localhost:5120/auth/callback` (dev, host-run and `docker-compose.dev.yml`/
  ADR-0022 — same port either way) and `http://localhost:8080/auth/callback`
  (`docker-compose.prod.yml`/ADR-0021), so all three local stacks can exercise a real magic-link
  login against this one local Supabase instance without touching Cloud.
- `Supabase:Url`/`Supabase:AnonKey` for local development now point at the local instance's fixed,
  well-known values (`http://127.0.0.1:54321` from the host, `http://host.docker.internal:54321`
  from inside a container — see below; the anon key is `supabase start`'s own published local
  demo key, not a secret) — set via `dotnet user-secrets` for host-run dev, or `backend/.env`'s
  `SUPABASE_URL`/`SUPABASE_ANON_KEY` for `docker-compose.dev.yml`.
- `backend/scripts/bootstrap-local-supabase-user.ps1` (new) creates a Supabase Auth user against
  the local instance via GoTrue's Admin API (idempotent — finds the existing user by email if
  already created) and upserts the matching row in the local Postgres `users` table, mirroring
  `bootstrap-production-user.ps1`'s pattern but without needing a pre-existing real Supabase UID
  (there's nothing to pre-provision locally — the script creates the Auth user itself).
- Two real code fixes were required in `AuthenticationServiceCollectionExtensions.cs` — both
  found by actually running the real login flow locally, not assumed:
  1. **`RequireHttpsMetadata`**: `JwtBearer` refuses a plain-HTTP `Authority` by default. Never
     hit before, because `Authority` had only ever been a real Supabase Cloud URL (always
     `https://`). Now conditioned on `Supabase:Url`'s own scheme — production is unaffected since
     its Url is always `https://`.
  2. **`LocalSupabaseOpenIdConfigurationRetriever`**: GoTrue's own OIDC discovery document always
     advertises `jwks_uri` as its own fixed, self-referential URL (`http://127.0.0.1:54321/...`
     for a local instance, since `external_url` is never configured per-consumer) — this is
     reachable from the host (`dotnet run`), but unreachable from inside the `api` container in
     `docker-compose.dev.yml`, which reaches the same instance via `host.docker.internal`, not
     `127.0.0.1` (that address means the container itself there). The container's own
     `Authority`-driven discovery fetch for `.well-known/openid-configuration` succeeds, but the
     follow-up fetch of `jwks_uri` (as advertised in that very document) then fails with
     `Connection refused (127.0.0.1:54321)`. `LocalSupabaseOpenIdConfigurationRetriever` still
     trusts the document's `Issuer` (a fixed string, valid regardless of which address reached
     it — it matches the token's own `iss` claim either way), but refetches signing keys from the
     caller's own known-reachable `Authority` instead of the document's self-reported `jwks_uri`.
     For a real Cloud Authority, Supabase always sets `external_url` to that same public URL, so
     this never diverges from default OIDC behaviour there — it's a strict superset of correctness,
     not a local-only special case.
- `host.docker.internal`, not `127.0.0.1`, for `Supabase:Url` inside `docker-compose.dev.yml`'s
  `api` container — the same class of Docker networking gotcha already documented for
  ADR-0022's Compose multi-file path resolution: a container's own `127.0.0.1` means the
  container itself, never the host running `supabase start`'s containers.
- Mailpit needs no configuration — `supabase start` bundles it and GoTrue's local config already
  points its own SMTP at it by default. Every magic-link email sent during local development
  lands at `http://127.0.0.1:54324`, viewable in a browser or fetched via its REST API
  (`GET /api/v1/messages`, `GET /api/v1/message/{id}`) for scripted/automated retrieval.

## Why

- Removing the Supabase Cloud dependency for development removes the single biggest reason local
  end-to-end testing had never actually been exercised before (`docs/current-state.md` had
  explicitly recorded "real Supabase magic-link login/logout... not proven" as a standing gap,
  even for the already-implemented Phase 6a local Docker runtime). This ADR closes that gap: a
  full magic-link login → session → refresh → logout cycle is now verified for real, against a
  real (if local) Supabase Auth instance, both from a host-run backend and from
  `docker-compose.dev.yml`'s containerized one.
- The two code fixes are not local-only workarounds bolted on top of the "real" Cloud-only path —
  both are strictly more correct treatments of the general OIDC-discovery contract (don't assume
  HTTPS; don't blindly trust a self-reported `jwks_uri` over your own known-reachable address) that
  happen to matter for Cloud not at all today, but would matter for the same reason if Supabase
  Cloud were ever reached through a proxy or gateway with a different externally-visible URL than
  Supabase's own `external_url`.
- Reusing `backend/docker-compose.yml`'s existing local Postgres for the app's own data, completely
  separate from the Supabase CLI's own internal Postgres (port 54322, used only by GoTrue/
  PostgREST internally), keeps the "app data is always local, only auth is ever external" split
  from ADR-0006 fully intact — this ADR only changes what "external" *means* during development
  (a local instance instead of a Cloud one), not the split itself.

## Consequences

- `supabase/config.toml` is checked in; the Supabase CLI's own runtime state
  (`supabase/.gitignore`'s `.branches`/`.temp`) is not.
- Anyone picking up this repo needs the Supabase CLI (`npx supabase@latest` works without a global
  install) and Docker running `supabase start` once before local login can work at all — documented
  in README.md "Local Supabase (Development)".
- `docker-compose.prod.yml` (ADR-0021) can now also be pointed at this same local Supabase
  instance (via `host.docker.internal`, same reasoning) to finally exercise its own deferred
  manual acceptance checklist without needing a real Cloud project either — not yet done in this
  change, left as a natural next step.
- E2E (Phase 6b, `docker-compose.e2e.yml`) is entirely unaffected — it already never touches
  Supabase, real or local, by design (`E2ESessionController`, `docs/testing/strategy.md` §7.6).

## Considered alternatives

- **Keep pointing dev at Supabase Cloud, add a password-login option instead.** Rejected per the
  user's explicit direction — this only would have removed the "wait for an email" friction, not
  the Cloud dependency itself, and the user wants zero internet dependency during development,
  reserving Cloud for production only.
- **Build a fully custom, Supabase-free local auth provider (password hash in the app's own
  Postgres).** Considered as an alternative to this ADR — would remove the Supabase dependency
  entirely rather than just making it local, at the cost of a second identity-verification
  implementation to keep behaviourally equivalent to production's Supabase Auth. Not pursued once
  `supabase start` was confirmed to give a genuinely offline, zero-cloud-credential local instance
  of the *same* system production uses — mirroring an already-real Auth backend end to end is more
  useful than mirroring what a hand-rolled one might do.
