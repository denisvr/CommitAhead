# CommitAhead — Deployment Strategy

## Current Local Runtime (Phase 6a)

| Concern | Platform | Notes |
|---|---|---|
| Application | Local Docker image via `docker-compose.prod.yml` | Kestrel serves the built React SPA and API from one container |
| Database | Local Docker PostgreSQL | Persistent named volume; reproducible roles, migrations, and RLS |
| Auth | Supabase Auth, when configured | Backend-mediated magic link + PKCE; no browser Supabase key |
| Storage | Supabase Storage, when configured | Private `job-postings` bucket; backend forwards the current user's JWT (ADR-0018) |
| Secrets | Gitignored environment file | Local production-like mechanism only, not the Phase 6c answer |

Phase 6a is a local production-like runtime, not an internet deployment. Its verified boundary and
manual acceptance checklist are in `README.md`; current priorities are in `docs/current-state.md`.

## Future Internet Deployment (Phase 6c — Deferred)

The target data services remain Supabase PostgreSQL, Auth, and private Storage, but the application
hosting and secrets platform are not decided. The Vite production build (`frontend/dist`) is copied
into the published ASP.NET Core artifact's `wwwroot` and served by Kestrel from the same origin as
the API. One container/process will be deployed. See `docs/tbd.md`; do not begin Phase 6c without
explicit user authorization.

## Requirements for the Chosen Platform

Whatever platform is selected must satisfy:

- **TLS termination** — HTTPS enforced everywhere; HSTS enabled in production
- **Environment variables / secrets injection** — API key, DB connection string, Supabase keys,
  and Data Protection key ring must be injectable as environment variables or mounted secrets
  (never baked into the image)
- **Single-process deployment** — Kestrel serves the React production assets and API from one
  ASP.NET Core process; no multi-instance load balancing is currently needed
- **Pre-deploy migrations** — a reviewed EF Core migration bundle runs before the new API accepts
  traffic; the application never migrates its own schema on startup
- **Container support** (preferred) — Dockerfile-based deployment for reproducibility; enables
  Trivy image scanning in CI/CD
- **Cost** — proportionate to a small, invite-only user base; minimal idle cost acceptable

## Data Protection Key Ring

Phase 6a persists ASP.NET Data Protection keys to a local named volume. Phase 6c must additionally
encrypt and persist them in a durable location appropriate to the chosen platform. That decision
remains open in `docs/tbd.md`.

## Internet Deployment Flow (Target, Not Started)

1. CI builds and tests the application.
2. Docker image is built and scanned with Trivy; high/critical findings block deployment.
3. An SBOM is generated and archived.
4. The image is pushed to a container registry.
5. A reviewed EF Core migration bundle applies pending migrations using the migration credential.
6. The new container is deployed and its health check passes.
7. The pre-internet-deployment security checks complete.

## Environment Boundaries

| Environment | Purpose | AI calls |
|---|---|---|
| Local development | Fast feature iteration | Real provider optional and called only by an explicit Analyze action; automated tests use fakes/stubs |
| Local production-like (Phase 6a) | Exercise the deployable image locally | Real provider only when configured and explicitly invoked |
| Isolated local E2E (Phase 6b) | Four explicit Playwright journeys | Real Anthropic adapter against the deterministic local stub; no real external calls |
| CI | Automated tests | `FakeAIProvider` or stubbed HTTP only; no real external AI |
| Internet production (Phase 6c) | Future live use | Real configured `IAIProvider`; deferred |

The Phase 6b E2E stack is local, isolated, and non-persistent. It is not staging and must never
connect to Phase 6a data or real Supabase/Anthropic services.
