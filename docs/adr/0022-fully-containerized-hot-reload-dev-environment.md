---
status: accepted
date: 2026-08-17
---

# A fully-containerized, hot-reload dev environment, layered on the existing dev Postgres

## Context

Before this ADR, "local development" meant: Postgres in Docker (`backend/docker-compose.yml`),
but the API and frontend ran directly on the host (`dotnet run` / `npm run dev`), requiring the
.NET SDK and Node.js installed locally. Asked directly, the requirement was explicit: not
production (ADR-0021 already covers that, deliberately without hot-reload — it validates the
shipped image, and rebuilding on every change is the point, not a limitation to fix), but a dev
environment that is "fácil, subindo em container" — easy, by bringing everything up in containers
— where editing code reflects automatically, without a rebuild.

This is a distinct requirement from both existing stacks: ADR-0021's `docker-compose.prod.yml` is
deliberately production-shaped (one built image, `ASPNETCORE_ENVIRONMENT=Docker`, no source
mount); the E2E stack (`docker-compose.e2e.yml`) is deliberately isolated and non-persistent. This
ADR does not replace either.

## Decision

**`docker-compose.dev.yml`** (repo root) adds `db-init`, `api`, and `frontend` services, layered
**on top of** `backend/docker-compose.yml`'s existing `db` via Compose's multi-file merge —
`docker compose -f backend/docker-compose.yml -f docker-compose.dev.yml up -d --build` — rather
than duplicating a second Postgres definition. Compose derives its default project name from the
first `-f` file's own directory, so keeping `backend/docker-compose.yml` listed first means both
this stack and the pre-existing host-run workflow resolve to the identical project name and
therefore the same database/volume — switching between "run the API in my IDE" and "run everything
in containers" never means switching data.

- **`db-init`** is a one-shot initializer — roles → self-contained EF migration bundle → RLS —
  built from `backend/scripts/db-init/` (Dockerfile + script), run once per `docker compose up`
  and exiting 0. This is the same *approach* the E2E stack's own `e2e/support/db-init/` already
  proved (a self-contained `linux-x64` migration bundle needs no .NET SDK on the host at all), but
  a **deliberately separate copy**, not a shared file: `e2e/support/db-init/` is E2E-owned per
  `docs/testing/strategy.md` §7.11, and ADR-0021 already established that this project's local
  runtimes must stay genuinely separate environments from the isolated E2E stack. Reusing the same
  build steps is fine; reusing the same file blurs an ownership boundary this project is otherwise
  careful about. `api` depends on `db-init` completing successfully, so it never starts against a
  half-migrated database.
- **`api`** runs `dotnet watch run` directly from the pinned `mcr.microsoft.com/dotnet/sdk:10.0.302`
  image (the same version `backend/global.json` requires), with `./backend` bind-mounted as a
  volume and a named volume for the NuGet package cache (so packages aren't re-restored from
  scratch on every `docker compose up`). `ASPNETCORE_ENVIRONMENT=Development`, so it reads
  `appsettings.Development.json` exactly like a host `dotnet run` — including
  `Auth:CallbackUrl=http://localhost:5120/auth/callback`, which needs no change here, because the
  container publishes the same port (`127.0.0.1:5120:8080`) `launchSettings.json` already uses on
  the host. `Supabase:Url`/`Supabase:AnonKey`/the Anthropic API key come from optional
  `backend/.env` entries (new, alongside the existing Postgres passwords) rather than
  `dotnet user-secrets` — a container has no access to the host's user-secrets store, and `.env`
  (already gitignored) is the equivalent already established for `docker-compose.prod.yml`. All
  three stay optional: an unconfigured value fails the same use case closed with a clear error,
  exactly like running unconfigured on the host today — never a silent fallback.
- **`frontend`** runs `npm ci && npm run dev -- --host 0.0.0.0` directly from `node:24-alpine` (the
  version `frontend/.nvmrc` pins), with `./frontend` bind-mounted and a named volume specifically
  for `node_modules` mounted *over* the bind mount's own `node_modules` path — otherwise the host's
  (likely absent, or wrong-OS-native-binary) `node_modules` would shadow whatever `npm ci` installs
  inside the container. `vite.config.ts`'s dev proxy target becomes configurable via
  `VITE_DEV_API_PROXY_TARGET` (new env var, defaulting to the existing `http://localhost:5120` so
  the host-run workflow is completely unchanged) — inside the container, `localhost` would mean the
  frontend container itself, so this stack sets it to `http://api:8080`, the Compose service name.

**No host .NET SDK and no host Node.js are required at all** to bring this stack up — every build
and run step happens inside its own image. This is the actual "fácil" requirement: `git clone`,
install Docker, copy one `.env` file, one `docker compose` command.

## Why

- **Share the database, not just the intent.** Layering onto `backend/docker-compose.yml` instead
  of duplicating a `db` service means there is exactly one source of dev data regardless of which
  workflow (host or containers) touched it last — the alternative (a second Postgres/volume) would
  silently fork state the moment someone used both workflows in the same week.
- **Reuse the db-init *pattern*, not the E2E stack's own file.** The self-contained migration
  bundle approach already earned its keep in the E2E foundation; copying the two files that
  implement it costs ~50 lines of duplication and buys a real, already-stated project rule (E2E
  stays a separate environment, not shared infrastructure) staying true in practice, not just in
  prose.
- **Hot-reload via bind mounts + watch/dev-server processes, not a custom rebuild loop.**
  `dotnet watch` and Vite's own dev server already solve "reflect a code change immediately" —
  building a container-specific alternative would be reinventing tooling that already exists and is
  already relied on for the host-run workflow.
- **Optional Supabase/Anthropic config, not a container-specific auth story.** Real backend-mediated
  Supabase Auth (ADR-0015 et al.) already works from `dotnet run`; this stack must not invent a
  parallel local-auth mechanism — it only needed a container-reachable way to supply the same
  configuration `dotnet user-secrets` supplies on the host.

## Consequences

- `README.md` documents this as a second, equally-supported dev workflow alongside the existing
  host-run one ("Local Database (Development)") — not a replacement. Contributors choose per
  their own setup (IDE debugging vs. "just make it run").
- Session cookies/Data Protection keys are not persisted across `api` container recreation (no
  `DataProtection:KeyRingPath` is set here, matching plain `Development` behavior) — restarting the
  `api` service signs everyone out, same as restarting a host `dotnet run` process today. Not a
  regression this ADR introduces; not something this ADR needed to fix either.
- `backend/scripts/db-init/` and `e2e/support/db-init/` will drift slightly over time (e.g. if a
  future migration needs a new RLS script copied into both `COPY` lists) — an accepted, explicit
  cost of keeping the two environments' build files independent, per "Why" above.

## Considered alternatives

- **A single shared `db-init` under a neutral path** (e.g. `backend/scripts/shared-db-init/`),
  referenced by both `docker-compose.e2e.yml` and `docker-compose.dev.yml` — rejected: it would
  require editing `docker-compose.e2e.yml` and `docs/testing/strategy.md`'s own file-ownership
  table for a stack that was explicitly out of scope for this change, to save copying two small,
  rarely-changed files.
- **Custom file-watcher script that copies changed files into a running production-shaped
  container** — rejected: reinvents `dotnet watch`/Vite's dev server, both of which already exist,
  are already used by the host-run workflow, and handle framework-specific reload semantics (e.g.
  which file changes require a full process restart vs. a hot module swap) correctly already.
- **Keep requiring the host .NET SDK/Node.js for this workflow too** (e.g. bind-mount source but
  still run `dotnet`/`npm` from the host into the container via some other mechanism) — rejected:
  defeats the explicit "no host tooling" requirement; the whole point was that only Docker itself
  needs to be installed.
