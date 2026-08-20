# CommitAhead

Private, invite-only professional profile and CV presentation app — data is isolated per user by `OwnerUserId` from the start (see ADR-0015); public signup stays disabled. Today there is exactly one real user. Before planning work, read `docs/current-state.md` for the current status, priority, and explicit deferrals. Read `CONTEXT.md` for terminology and `docs/adr/` for why key decisions were made before suggesting alternatives.

## Stack
- **Frontend:** React 19 + TypeScript + Vite; OpenAPI-generated TypeScript client
- **Backend:** ASP.NET Core 10 Web API — Controllers, feature-folder use cases, no MediatR, no Minimal APIs
- **ORM:** EF Core 10 + Npgsql
- **Database:** PostgreSQL in Docker for development and the Phase 6a local runtime; Supabase PostgreSQL is the deferred Phase 6c internet target
- **Auth:** Supabase (backend-mediated — no Supabase keys in the browser); local via the Supabase CLI (`supabase start`) in development, Cloud only in production (ADR-0023)
- **Hosting:** TBD — see `docs/tbd.md`

## Hard constraints
- No MediatR, no Minimal APIs, no generic `IUseCase<T>` interfaces
- All Supabase keys are backend-only

## Clean Architecture layers

| Layer | Project | Allowed dependencies |
|---|---|---|
| Domain | `CommitAhead.Domain` | None (no framework, no EF Core, no Supabase) |
| Application | `CommitAhead.Application` | Domain only — no EF Core, no Npgsql, no ASP.NET Core, no Supabase |
| Infrastructure | `CommitAhead.Infrastructure` | Domain + Application; owns EF Core, Npgsql, Supabase SDK |
| API | `CommitAhead.Api` | Application plus Infrastructure only at the composition root (`Program.cs` / DI registration); controllers depend on Application only |
| Frontend | `frontend/` | Backend via OpenAPI-generated client only |

**Layer responsibilities:**
- **Domain** — aggregates, value objects, domain invariants
- **Application** — one use case class per operation (`CreateProfessionalProfileUseCase`, `ExportCVPresentationUseCase`, …); orchestrates domain + repositories; contains repository interfaces
- **Infrastructure** — EF Core `DbContext`, repository implementations, the CV export renderer (`QuestPdfCVExportRenderer`, ADR-0020)
- **API** — thin controllers calling use cases directly; middleware for auth, CSRF, error mapping, logging; no business logic. The composition root may call Infrastructure DI registration, but controllers may not reference Infrastructure types.

**NetArchTest enforces** (5 rules):
1. Domain has no dependency on Application, Infrastructure, or API.
2. Application has no dependency on Infrastructure, API, EF Core, Npgsql, ASP.NET Core, or Supabase.
3. Infrastructure has no dependency on API.
4. Controllers depend on Application only — not Infrastructure, repositories, `DbContext`, or domain services. The API composition root is the explicit exception for Infrastructure registration.
5. Repository production implementations exist only in Infrastructure (test fakes excluded) — checked against an explicit, named list of every persistence port declared in Application: `IUserRepository`, `IRlsSessionContext`, `IProfessionalProfileRepository`, `ICVPresentationRepository` (`ArchitectureTests.PersistencePorts`).

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
│   ├── current-state.md              ← concise operational handoff and current priority
│   ├── roadmap.md
│   └── tbd.md
├── backend/                           ← ASP.NET Core solution — not the frontend
│   ├── CommitAhead.slnx
│   ├── global.json
│   ├── docker-compose.yml             ← dev Postgres only; shared by host-run dev and
│   │                                     docker-compose.dev.yml (ADR-0022) via multi-file layering
│   ├── scripts/db-init/               ← dev-only migration-bundle init (own copy, not shared
│   │                                     with e2e/support/db-init/ — see ADR-0022)
│   ├── scripts/bootstrap-local-supabase-user.ps1 ← seeds a local Supabase Auth user (ADR-0023)
│   ├── src/
│   │   ├── CommitAhead.Domain/
│   │   ├── CommitAhead.Application/
│   │   ├── CommitAhead.Infrastructure/
│   │   └── CommitAhead.Api/
│   └── tests/
│       ├── CommitAhead.Domain.Tests/
│       ├── CommitAhead.Application.Tests/
│       ├── CommitAhead.Infrastructure.Tests/
│       └── CommitAhead.Api.Tests/
├── frontend/                          ← React 19 + Vite app — a separate application, not a
│   ├── package.json                     Clean Architecture layer; builds to frontend/dist
│   ├── src/
│   └── tests (colocated with src, e.g. src/App.test.tsx)
├── docker-compose.dev.yml             ← fully-containerized hot-reload dev (ADR-0022); layers onto
│                                         backend/docker-compose.yml's db, never used alone
├── supabase/config.toml               ← local Supabase instance config (ADR-0023); `supabase start`
│                                         runs it entirely via Docker, no Cloud project needed in dev
├── docker-compose.e2e.yml             ← isolated E2E stack; only `proxy` is host-facing
└── e2e/                               ← Playwright suite — foundation implemented, both approved
    ├── playwright.config.ts             journeys written and passing; own package.json, never in
    ├── scripts/                          the app dependency tree
    │   run-full.mjs (lifecycle), reset-db.mjs (only reset path), verify-foundation.mjs
    ├── support/                       ← reset.sql, db-init/ (roles→bundle→RLS), external-stub/
    └── tests/                         ← fixtures/e2e-test.ts, journeys/001, 004
```

`frontend/dist` is never committed and never copied into `backend/src`. It is copied into the
published backend artifact's `wwwroot` only during `dotnet publish` (see the
`CopyFrontendBuildToPublishOutput` MSBuild target in `CommitAhead.Api.csproj`).

## Frontend design contract

- The approved identity is **Studio** with the **Bookmark** mark (ADR-0024, superseding Reading
  Room). The canonical design documentation is `docs/design/design-system/readme.md`.
- Before frontend work, read that document plus `components.md` and `page-patterns.md` in the same
  directory. `CONTEXT.md` and the domain/ADR documents remain authoritative for behaviour and
  terminology whenever a visual reference disagrees with them.
- `frontend/src/design-system/tokens/` is a copy of `docs/design/design-system/tokens/` and carries
  a header saying so. The design reference is the source of truth: change a value there and copy it
  across in the same PR — never edit only one side.
- Light and dark are one system, not two palettes. Any new colour token must be added to both the
  `:root[data-theme="dark"]` block and the `prefers-color-scheme` block, and
  `node docs/design/design-system/verify-contrast.mjs` must pass. Review every screen in light, in
  dark, and with no explicit choice under a dark system preference.
- Implement production UI as React 19 + TypeScript components with CSS Modules and shared CSS
  custom-property tokens, according to ADR-0016. No Tailwind, MUI, shadcn, CSS-in-JS, inline
  `style` attributes, CDN assets, runtime-injected SVG sprites, or `window` globals.
- Copy approved tokens and selected local assets into `frontend/src/design-system/` when the first
  production slice needs them. The files under `docs/design/` are design references, not runtime
  dependencies.
- Build components incrementally for the current roadmap slice. Do not pre-build every documented
  screen or component, and do not implement later-phase behaviour from a mock.
- Reuse production design-system components and tokens. Do not introduce page-local colour
  palettes, spacing scales, radii, shadows, or duplicate primitives.
- Preserve semantic HTML, complete keyboard operation, visible focus, responsive behaviour and
  the CSP in `docs/security/threat-model.md`. Values computed by the backend, including CV
  export eligibility (template, photo, page limit), are rendered from API responses and never
  recomputed in React.

## E2E testing contract

- E2E is never part of ordinary feature development or PR validation.
- Run it only when explicitly requested, or when directly changing something under `e2e/`.
- Before touching `e2e/` or the E2E Docker stack, read `docs/testing/strategy.md` Layer 7 (the
  normative contract) and `e2e/README.md` (the operational runbook) first.
- Zero real Supabase calls, ever — the isolated stack replaces it.
- Exactly two approved journey files under `e2e/tests/journeys/` — no more, no fewer.

## CI quality gates (every PR — all blocking)

**Build & static analysis:**
- `dotnet build --warnaserror` (warnings as errors)
- `vite build` (production frontend build)
- `dotnet format --verify-no-changes`
- ESLint
- ESLint blocks JSX `style` attributes (ADR-0016 / production CSP)
- `tsc --noEmit`
- Regenerate + compile OpenAPI TypeScript client (contract drift detection)

**Security scans:**
- `dotnet list package --vulnerable` + `npm audit --audit-level=high` (direct + transitive)
- Gitleaks secret scanning

**Tests:**
- Domain unit tests
- Application use-case tests (handwritten fakes)
- Repository / integration tests (Testcontainers PostgreSql + Respawn, serial) — includes applying `001_roles.sql`/`002_rls_users.sql`/`004_rls_phase2.sql` against a disposable database and proving RLS isolation end to end (`RlsIsolationPhase2Tests`); those scripts are never run by the production application itself, but CI does run them
- API tests (WebApplicationFactory + shared Testcontainers DB)
- NetArchTest architecture rules
- Security API tests (auth, CSRF, CSP, CORS, `Cache-Control: no-store`, log redaction)
- Frontend/export security tests for restricted Markdown rendering and dangerous-link protocols
- Parsed CV export assertions

**Post-merge / manual only:**
- Playwright E2E — foundation implemented and both approved journeys passing (see "E2E testing contract" above and `docs/roadmap.md` Phase 6b) — still post-merge/manual only, never a blocking PR gate
- Visual regression fixtures (per CV template) — implemented (`QuestPdfCVExportRendererVisualRegressionTests`); regenerating a baseline after an intentional template change is a separate, explicitly-run test, never automatic
- SBOM generation + Trivy container scan (high/critical blocks deployment)
- OWASP ZAP baseline (fail on confirmed high-severity)
