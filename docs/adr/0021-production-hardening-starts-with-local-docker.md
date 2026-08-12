---
status: accepted
date: 2026-08-12
---

# Phase 6 starts with a hosting-neutral local Docker deployment, not a cloud platform

## Context

`docs/tbd.md` blocked Phase 6 ("Production Hardening") on hosting platform, secrets management,
Data Protection key storage, backup retention, and log retention — all real infrastructure
decisions with cost and operational consequences. Asked directly, the answer was: defer the cloud
platform choice entirely for now. The near-term goal is a production-like deployment that can be
built, run, and used extensively on the developer's own machine via Docker, with no cloud
provider, secrets manager, or hosting-specific configuration anywhere in it. Cloud hosting (and the
decisions that depend on it — secrets manager, Data Protection key storage at rest, encrypted
backups, centralized logging) is decided only after that local deployment has been validated.

This ADR covers only what that local-first slice actually needs to be real (not a stub): a
production Docker image, Docker Compose for the app plus its own PostgreSQL, Data Protection keys
that survive a restart, environment-based configuration, and a working migration path — while
staying strictly portable to whatever hosting platform comes next.

## Decision

**A single multi-stage `Dockerfile`** (repo root, build context the repo root) produces one
portable image: a Node stage builds the frontend, a pinned .NET SDK stage publishes the backend
(which copies the frontend build into `wwwroot`, per the existing `CommitAhead.Api.csproj` publish
target), and a minimal ASP.NET Core runtime stage runs it as a non-root user. No cloud-provider
base image, SDK, or CLI is installed in any stage.

- The SDK stage is pinned to the *exact* version `backend/global.json` requires
  (`mcr.microsoft.com/dotnet/sdk:10.0.302`), not the floating `10.0` tag. `global.json`'s default
  `rollForward` policy (`latestPatch`) only rolls forward within the same feature band — a floating
  tag that resolves to a later band (e.g. `10.0.4xx`) fails `dotnet publish` inside the image with
  an SDK-not-found error, discovered by actually building the image, not assumed.
- The runtime stage exposes `/api/health` (already implemented, `[AllowAnonymous]`) as a Docker
  `HEALTHCHECK`, reusing the same endpoint rather than adding a second health surface.

**`docker-compose.prod.yml`** (repo root) runs the app plus a dedicated PostgreSQL, both with named
volumes and `restart: unless-stopped`, sitting alongside `backend/docker-compose.yml` (dev-only
Postgres) without conflict — different project name, ports (5434 vs 5433), and volumes. All
configuration is environment variables (`backend/.env.production`, gitignored, templated by
`backend/.env.production.example`) — no hosting-specific secrets integration yet; that is exactly
the still-open decision this ADR does not resolve.

**`ASPNETCORE_ENVIRONMENT=Docker`** is a new, distinct environment name for this stack — not
`Development` (which enables the dev CORS policy and OpenAPI endpoint neither needed here) and not
a generic `Production` (which would enable `UseHsts()`/`UseHttpsRedirection()`, and this stack has
no TLS termination of its own — a real deployment behind a TLS-terminating reverse proxy/load
balancer would use `Production` and keep both). `Program.cs` skips both specifically for this one
environment name; every other non-Development environment keeps them. Auth/CSRF cookies needed no
change: they already read `Secure = true` unconditionally, and every modern browser treats
`http://localhost` as a secure context regardless of scheme, so they are still sent when this stack
is reached at `http://localhost:8080` — no change was needed there, verified against actual browser
behavior rather than assumed.

**Data Protection keys persist to a mounted volume**, not the default ephemeral/per-machine store.
`AddCommitAheadSecurity` now reads an optional `DataProtection:KeyRingPath` configuration value and
calls `PersistKeysToFileSystem` when set; `docker-compose.prod.yml` sets it to `/keys`, backed by a
named volume, so cookie-encryption keys — and therefore existing sessions — survive a container
restart. The keys are **not encrypted at rest** here (that needs a cloud KMS or protected-key
integration, still the open "Data Protection key ring storage" decision in `docs/tbd.md`) — this
closes the "restart invalidates every session" problem only, not the encryption-at-rest problem.

**Migrations get a portable, reviewed artifact**: `backend/scripts/build-migration-bundle.ps1` runs
`dotnet ef migrations bundle --self-contained -r linux-x64`, producing a single executable
(`backend/artifacts/efbundle`, gitignored) that applies pending migrations without the .NET SDK
installed on the target — the actual deliverable behind the roadmap's "reviewed EF migration
bundle" item. For this local stack specifically, `backend/scripts/setup-production-db.ps1` (mirrors
`setup-local-db.ps1` exactly, targeting `docker-compose.prod.yml`'s db service on port 5434 instead)
still uses `dotnet ef database update` directly, since the SDK is already on the developer's own
machine — the bundle is for wherever that assumption stops holding, i.e. a real deployment target.

**The application's own PostgreSQL stays local for now.** The real Supabase Postgres project
(already provisioned for Auth) is not migrated against yet — that remains the deliberate, documented
gap in `README.md` ("Setting Up the Real Supabase Project"), now explicitly re-scoped to the
cloud-deployment stage rather than "Phase 6" in general.

## Why

- **Validate the actual deployment artifact before choosing where it runs.** A production Docker
  image and Compose stack are the same regardless of the eventual host (Fly.io, Railway, a VPS,
  anything Docker-compatible) — building and using them now surfaces real problems (SDK pinning,
  cookie behavior, key persistence, migration sequencing) while they are still cheap to fix, instead
  of discovering them for the first time against a real cloud bill.
- **No premature infrastructure lock-in.** Every hosting-specific decision this ADR does not make
  (secrets manager, encrypted-at-rest key storage, managed backups, centralized log retention) stays
  genuinely open in `docs/tbd.md` — nothing here assumes or biases toward a particular platform.
- **A single environment-name gate, not scattered feature flags.** `ASPNETCORE_ENVIRONMENT=Docker`
  is one flag Program.cs branches on twice (HSTS/redirect); no provider-specific `#if`/config
  sprawl, and the two other environments (`Development`, everything else) are completely unchanged.

## Consequences

- `docs/tbd.md`'s hosting/secrets/Data-Protection/backup/log-retention entries stay open, annotated
  with the target policies already decided (30-day log retention; 30-day backup retention with a
  quarterly restore test) for whenever the cloud-deployment stage implements them — this ADR does
  not close any of them.
- A future ADR is needed once a hosting platform is actually chosen, covering at minimum: secrets
  injection method, encrypted-at-rest Data Protection key storage (or a switch to a managed
  alternative), automated encrypted backups covering both Postgres and Supabase Storage, and
  centralized log shipping with the retention policy above.
- `docker-compose.prod.yml`'s `db` service is not the target for the real Supabase Postgres —
  whenever that migration happens, `app`'s `ConnectionStrings__CommitAheadDb` points there instead
  and this stack's own `db` service becomes optional/removable, not a decision this ADR makes now.

## Considered alternatives

- **Pick a hosting platform now and deploy directly** — skips the local validation step entirely;
  rejected because the explicit ask was to validate the container and its behavior locally first,
  and because committing to a platform before using the app in a production-like shape risks paying
  for and configuring the wrong thing.
- **Terminate TLS locally with a self-signed certificate** instead of gating HSTS/redirect off for
  one environment name — would let every environment share identical HTTPS-enforcement code, but
  adds certificate generation/trust and Kestrel HTTPS-endpoint configuration that a real deployment
  will replace with a reverse proxy anyway; rejected as complexity this local-validation slice does
  not need.

## Amendment: making the local stack genuinely usable

The first version of this stack built and started, but could not actually be *used*: nobody could
sign in (no `redirect_to` was ever sent to Supabase, and no `User` row existed after a fresh
migration for closed login — ADR-0015 — to accept), the ports were reachable from the whole LAN
rather than just this machine, "container limits are the backstop" was aspirational text rather
than a real limit, and the roadmap's own claim of a manual backup command didn't correspond to an
actual script. Fixed, still within this ADR's own hosting-neutral scope:

- **Configurable, per-environment Supabase callback.** `AuthOptions.CallbackUrl` (new, bound from
  the "Auth" configuration section) is sent as a percent-encoded `redirect_to` **query parameter**
  on the magic-link `/auth/v1/otp` request (`SupabaseAuthClient.InitiateMagicLinkAsync`) — GoTrue
  reads `redirect_to` only from the query string, never the JSON body, so an earlier version of
  this fix that put it in the OTP/PKCE JSON payload was silently ignored by Supabase; caught and
  corrected before it shipped further. Sourced only from trusted backend configuration, never
  derived from a request's Origin/Referer. Local `dotnet run` uses
  `http://localhost:5120/auth/callback` (`appsettings.Development.json`); the Docker stack uses
  `http://localhost:8080/auth/callback` (`docker-compose.prod.yml`'s `Auth__CallbackUrl`). Both must
  be present in the Supabase project's own redirect allow-list (README.md now documents both, not
  just the dev one).
- **Idempotent local user bootstrap.** `backend/scripts/bootstrap-production-user.ps1`
  inserts/updates exactly one row in the local PostgreSQL `users` table after migrations, reading
  `INITIAL_USER_ID`/`INITIAL_USER_EMAIL` from `backend/.env.production` (or explicit parameters). It
  never creates a Supabase account, never enables public signup, and never hardcodes a specific
  person — the Supabase Auth user with that id must already exist in the real project. Without
  this, closed login has nothing to ever match against on a fresh database.
- **Loopback-only ports.** `127.0.0.1:8080:8080` and `127.0.0.1:5434:5432`, not `0.0.0.0` — this
  stack is for local use on the machine running it, not for being reachable from the rest of the
  network.
- **Real container resource limits.** `app`'s `deploy.resources.limits` (1 CPU / 1 GiB) makes the
  threat model's own claim — "container memory/CPU limits are the real backstop" against a runaway,
  uncancellable PDF parse — actually true for this stack, not just asserted in prose.
- **A working manual backup command, not a promise of one — and a lossless, ownership-safe one.**
  `backend/scripts/backup-production-db.ps1` / `restore-production-db.ps1` replace what had been an
  unfulfilled reference to "a simple manual `pg_dump`" in `docs/roadmap.md`/`docs/tbd.md`. The first
  real version used `pg_dump --format=plain` piped through PowerShell and written with
  `Out-File -Encoding ascii` — deliberately lossy for anything outside ASCII, which this project's
  own user-entered content (e.g. Portuguese profile text) routinely is; caught before it shipped
  further and replaced with `pg_dump --format=custom` run entirely inside the `db` container,
  copied out as a raw binary file with `docker compose cp` (never through a PowerShell text stream).
  Restore (`pg_restore --clean --if-exists`, also run inside the container) deliberately keeps
  ownership/grants in the dump rather than passing `--no-owner`/`--no-privileges` — this backs up
  and restores against the *same* stack's own roles, so preserving them is what puts tables back
  under `commitahead_migrator` (required for future EF migrations) and grants back on
  `commitahead_app` (required for the app to run) after a restore, not just the row data; RLS
  policies come back automatically as ordinary table metadata. The script stops `app` for the
  restore and restarts it (`docker compose up -d app`) afterward, then verifies `commitahead_app`
  can still connect. Verified with a real round trip containing Portuguese accented text
  ("Experiência, educação, São Paulo"): backed up, mutated the row, restored, confirmed the exact
  original text came back (not the mutation), confirmed ownership/grants were byte-for-byte
  identical to before the round trip, and confirmed the app container was healthy afterward.
- **Reject placeholder credentials.** `setup-production-db.ps1` now refuses to bootstrap a
  "production-like" database still holding `.env.production.example`'s own `change_me` values for
  any required credential.

None of this changes the ADR's own boundary: still no hosting provider, no TLS infrastructure, no
centralized logging, no cloud secrets management, no automated cloud backups.
