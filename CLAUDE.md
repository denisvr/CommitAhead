# CommitAhead

Private single-user interview preparation app. Full domain model and architecture confirmed — read `CONTEXT.md` for terminology and `docs/adr/` for why key decisions were made before suggesting alternatives. See `docs/` for the complete product, domain, architecture, testing, and security documentation.

## Stack
- **Frontend:** React 19 + TypeScript + Vite; OpenAPI-generated TypeScript client
- **Backend:** ASP.NET Core 10 Web API — Controllers, feature-folder use cases, no MediatR, no Minimal APIs
- **ORM:** EF Core 10 + Npgsql
- **Database:** PostgreSQL on Supabase
- **Auth/Storage:** Supabase (backend-mediated — no Supabase keys in the browser)
- **AI:** `IAIProvider` abstraction; provider TBD; never called from frontend or domain layer
- **Hosting:** TBD — see `docs/tbd.md`

## Hard constraints
- No MediatR, no Minimal APIs, no generic `IUseCase<T>` interfaces
- AI commands produce `AnalysisDraft`s requiring per-proposal human confirmation — AI never writes to domain entities directly
- Zero real AI calls in CI — `FakeAIProvider` only in automated tests (absolute rule)
- All Supabase keys and the AI provider key are backend-only
- `EffectiveScore` is computed on-the-fly in the ranked-list query — not persisted on `StudyItem`

## Clean Architecture layers

| Layer | Project | Allowed dependencies |
|---|---|---|
| Domain | `CommitAhead.Domain` | None (no framework, no EF Core, no Supabase) |
| Application | `CommitAhead.Application` | Domain only — no EF Core, no Npgsql, no ASP.NET Core, no Supabase |
| Infrastructure | `CommitAhead.Infrastructure` | Domain + Application; owns EF Core, Npgsql, Supabase SDK, AI provider adapter |
| API | `CommitAhead.Api` | Application plus Infrastructure only at the composition root (`Program.cs` / DI registration); controllers depend on Application only |
| Frontend | `CommitAhead.Web` | Backend via OpenAPI-generated client only |

**Layer responsibilities:**
- **Domain** — aggregates, value objects, domain invariants, pure domain policies (e.g. `EffectiveScorePolicy`)
- **Application** — one use case class per operation (`CreateStudyItemUseCase`, `ApplyAnalysisDraftUseCase`, …); orchestrates domain + repositories; contains `IAIProvider` and repository interfaces
- **Infrastructure** — EF Core `DbContext`, repository implementations, AI provider adapter (`ProviderAIAdapter`, provider TBD), Supabase Storage client, PDF text extractor
- **API** — thin controllers calling use cases directly; middleware for auth, CSRF, error mapping, logging; no business logic. The composition root may call Infrastructure DI registration, but controllers may not reference Infrastructure types.

**NetArchTest enforces** (5 rules):
1. Domain has no dependency on Application, Infrastructure, or API.
2. Application has no dependency on Infrastructure, API, EF Core, Npgsql, ASP.NET Core, or Supabase.
3. Infrastructure has no dependency on API.
4. Controllers depend on Application only — not Infrastructure, repositories, `DbContext`, or domain services. The API composition root is the explicit exception for Infrastructure registration.
5. Repository and `IAIProvider` production implementations exist only in Infrastructure (test fakes excluded).

## Project structure (target)
```
CommitAhead/
├── README.md                         ← human entry point and documentation map
├── AGENTS.md                         ← instructions for coding agents
├── CONTEXT.md                        ← domain glossary
├── CLAUDE.md                         ← this file
├── docs/
│   ├── adr/                          ← architectural decisions
│   ├── product/                      ← brief, scope, out-of-scope
│   ├── domain/                       ← model, use cases, invariants
│   ├── architecture/                 ← solution, persistence
│   ├── testing/                      ← strategy
│   ├── security/                     ← threat model
│   ├── deployment/                   ← strategy (TBD)
│   ├── roadmap.md
│   └── tbd.md
├── src/
│   ├── CommitAhead.Domain/
│   ├── CommitAhead.Application/
│   ├── CommitAhead.Infrastructure/
│   ├── CommitAhead.Api/
│   └── CommitAhead.Web/              ← Vite React frontend
└── tests/
    ├── CommitAhead.Domain.Tests/
    ├── CommitAhead.Application.Tests/
    ├── CommitAhead.Infrastructure.Tests/
    ├── CommitAhead.Api.Tests/
    ├── CommitAhead.Web.Tests/         ← Vitest + RTL + MSW
    └── e2e/                           ← Playwright
```

## CI quality gates (every PR — all blocking)

**Build & static analysis:**
- `dotnet build --warnaserror` (warnings as errors)
- `vite build` (production frontend build)
- `dotnet format --verify-no-changes`
- ESLint
- `tsc --noEmit`
- Regenerate + compile OpenAPI TypeScript client (contract drift detection)

**Security scans:**
- `dotnet list package --vulnerable` + `npm audit --audit-level=high` (direct + transitive)
- Gitleaks secret scanning
- **Zero real AI calls** — `FakeAIProvider` enforced

**Tests:**
- Domain unit tests
- Application use-case tests (handwritten fakes)
- Repository / integration tests (Testcontainers PostgreSql + Respawn, serial)
- API tests (WebApplicationFactory + shared Testcontainers DB + `FakeAIProvider`)
- NetArchTest architecture rules
- Security API tests (auth, CSRF, CSP, CORS, `Cache-Control: no-store`, malicious uploads, AI schema validation, idempotency, rate/budget limits, log redaction)
- Frontend/export security tests for restricted Markdown rendering and dangerous-link protocols
- Parsed PDF/CV assertions

**Post-merge / manual only:**
- Playwright E2E (4 journeys)
- Visual regression fixtures (per CV template)
- SBOM generation + Trivy container scan (high/critical blocks deployment)
- OWASP ZAP baseline (FakeAIProvider, fail on confirmed high-severity)
- Live AI smoke tests (manual trigger, explicit cost ceiling, never scheduled)
