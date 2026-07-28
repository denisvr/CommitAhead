# CommitAhead — Deployment Strategy

## Decided

| Concern | Platform | Notes |
|---|---|---|
| Database | PostgreSQL on Supabase | Managed, encrypted at rest |
| Auth | Supabase Auth | Magic link + PKCE; JWKS endpoint for JWT validation |
| Storage | Supabase Storage | Private bucket; backend service-role access only |
| Secrets | TBD (see below) | `.NET User Secrets` for local dev |

## TBD

The hosting platform is not yet decided. The deployment topology is decided: the Vite production build is copied into the ASP.NET Core application and served by Kestrel from the same origin as the API. One container/process is deployed. See `docs/tbd.md` for the hosting-platform decision.

## Requirements for the Chosen Platform

Whatever platform is selected must satisfy:

- **TLS termination** — HTTPS enforced everywhere; HSTS enabled in production
- **Environment variables / secrets injection** — API key, DB connection string, Supabase keys, `OWNER_USER_ID`, Data Protection key ring must be injectable as environment variables or mounted secrets (never baked into the image)
- **Single-process deployment** — Kestrel serves the React production assets and API from one ASP.NET Core process; no multi-instance load balancing is needed
- **Pre-deploy migrations** — a reviewed EF Core migration bundle runs before the new API accepts traffic; the application never migrates its own schema on startup
- **Container support** (preferred) — Dockerfile-based deployment for reproducibility; enables Trivy image scanning in CI/CD
- **Cost** — proportionate to a private single-user app; minimal idle cost acceptable

## Data Protection Key Ring

ASP.NET Data Protection keys (used for cookie encryption and antiforgery) must be persisted to a durable location accessible across deployments (e.g. a mounted volume, Azure Blob, or AWS S3 — not in-memory, which rotates on restart). Configuration is infrastructure-dependent and TBD.

## Deployment Flow (target)

1. CI builds and tests the application (all PR gates pass).
2. Docker image built and scanned with Trivy; high/critical findings block deployment.
3. SBOM generated and archived.
4. Image pushed to container registry.
5. Pre-deploy: a reviewed EF Core migration bundle applies pending migrations using the separate migration credential.
6. New container deployed; health check passes.
7. Post-deploy: OWASP ZAP baseline scan against the deployed test environment.

## Environments

| Environment | Purpose | AI calls |
|---|---|---|
| Local development | Developer iteration | `FakeAIProvider` (real provider optional) |
| CI | Automated tests | `FakeAIProvider` only — no real AI |
| Staging / test | E2E, ZAP scan, smoke tests | `FakeAIProvider`; real provider for manual smoke tests only |
| Production | Live use | Real `IAIProvider` implementation |
