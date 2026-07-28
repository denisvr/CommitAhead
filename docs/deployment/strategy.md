# CommitAhead — Deployment Strategy

## Decided

| Concern | Platform | Notes |
|---|---|---|
| Database | PostgreSQL on Supabase | Managed, encrypted at rest |
| Auth | Supabase Auth | Magic link + PKCE; JWKS endpoint for JWT validation |
| Storage | Supabase Storage | Private bucket; backend service-role access only |
| Secrets | TBD (see below) | `.NET User Secrets` for local dev |

## TBD

The hosting platform for the **ASP.NET Core 10 API** and the **Vite/React frontend** is not yet decided. See `docs/tbd.md` for the open decision.

## Requirements for the Chosen Platform

Whatever platform is selected must satisfy:

- **TLS termination** — HTTPS enforced everywhere; HSTS enabled in production
- **Environment variables / secrets injection** — API key, DB connection string, Supabase keys, `OWNER_USER_ID`, Data Protection key ring must be injectable as environment variables or mounted secrets (never baked into the image)
- **Single-process deployment** — the API is a single ASP.NET Core process; no need for multi-instance load balancing (single-user app)
- **Migration run on startup** — `dotnet ef database update` (or equivalent) runs before the API starts accepting requests, or is run as a pre-deploy step
- **Container support** (preferred) — Dockerfile-based deployment for reproducibility; enables Trivy image scanning in CI/CD
- **Cost** — proportionate to a private single-user app; minimal idle cost acceptable

## Data Protection Key Ring

ASP.NET Data Protection keys (used for cookie encryption and antiforgery) must be persisted to a durable location accessible across deployments (e.g. a mounted volume, Azure Blob, or AWS S3 — not in-memory, which rotates on restart). Configuration is infrastructure-dependent and TBD.

## Deployment Flow (target)

1. CI builds and tests the application (all PR gates pass).
2. Docker image built and scanned with Trivy; high/critical findings block deployment.
3. SBOM generated and archived.
4. Image pushed to container registry.
5. Pre-deploy: `dotnet ef database update` applies any pending migrations.
6. New container deployed; health check passes.
7. Post-deploy: OWASP ZAP baseline scan against the deployed test environment.

## Environments

| Environment | Purpose | AI calls |
|---|---|---|
| Local development | Developer iteration | `FakeAIProvider` (real provider optional) |
| CI | Automated tests | `FakeAIProvider` only — no real AI |
| Staging / test | E2E, ZAP scan, smoke tests | `FakeAIProvider`; real provider for manual smoke tests only |
| Production | Live use | Real `IAIProvider` implementation |
