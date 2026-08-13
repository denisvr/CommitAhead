# CommitAhead

Private, invite-only interview preparation app — data is isolated per user by `OwnerUserId` from the start (see ADR-0015); public signup stays disabled. Today there is exactly one real user. Full domain model and architecture confirmed — read `CONTEXT.md` for terminology and `docs/adr/` for why key decisions were made before suggesting alternatives. See `docs/` for the complete product, domain, architecture, testing, and security documentation.

## Stack
- **Frontend:** React 19 + TypeScript + Vite; OpenAPI-generated TypeScript client
- **Backend:** ASP.NET Core 10 Web API — Controllers, feature-folder use cases, no MediatR, no Minimal APIs
- **ORM:** EF Core 10 + Npgsql
- **Database:** PostgreSQL on Supabase
- **Auth/Storage:** Supabase (backend-mediated — no Supabase keys in the browser)
- **AI:** `IAIProvider` abstraction; Anthropic, Claude Haiku 4.5 (ADR-0019); never called from frontend or domain layer
- **Hosting:** TBD — see `docs/tbd.md`

## Hard constraints
- No MediatR, no Minimal APIs, no generic `IUseCase<T>` interfaces
- AI commands produce `AnalysisDraft`s requiring per-proposal human confirmation — AI never writes to domain entities directly
- Zero real *external* AI calls in any automated test (absolute rule). Layers 1–6 use
  `FakeAIProvider` or stubbed HTTP. **Layer 7 (E2E) is the one documented exception to the
  mechanism, not the rule**: it runs the real `AnthropicAIProvider` against a deterministic local
  HTTP stub inside the E2E stack, because `FakeAIProvider` lives in test assemblies and cannot be
  reached from the production image. Nothing leaves the machine either way — see
  `docs/testing/strategy.md` §7.6
- All Supabase keys and the AI provider key are backend-only
- `EffectiveScore` is computed on-the-fly in the ranked-list query — not persisted on `StudyItem`

## Clean Architecture layers

| Layer | Project | Allowed dependencies |
|---|---|---|
| Domain | `CommitAhead.Domain` | None (no framework, no EF Core, no Supabase) |
| Application | `CommitAhead.Application` | Domain only — no EF Core, no Npgsql, no ASP.NET Core, no Supabase |
| Infrastructure | `CommitAhead.Infrastructure` | Domain + Application; owns EF Core, Npgsql, Supabase SDK, AI provider adapter |
| API | `CommitAhead.Api` | Application plus Infrastructure only at the composition root (`Program.cs` / DI registration); controllers depend on Application only |
| Frontend | `frontend/` | Backend via OpenAPI-generated client only |

**Layer responsibilities:**
- **Domain** — aggregates, value objects, domain invariants, pure domain policies (e.g. `EffectiveScorePolicy`)
- **Application** — one use case class per operation (`CreateStudyItemUseCase`, `ApplyAnalysisDraftUseCase`, …); orchestrates domain + repositories; contains `IAIProvider` and repository interfaces
- **Infrastructure** — EF Core `DbContext`, repository implementations, AI provider adapter (`ProviderAIAdapter`, Anthropic Claude Haiku 4.5 — ADR-0019), Supabase Storage client, PDF text extractor
- **API** — thin controllers calling use cases directly; middleware for auth, CSRF, error mapping, logging; no business logic. The composition root may call Infrastructure DI registration, but controllers may not reference Infrastructure types.

**NetArchTest enforces** (5 rules):
1. Domain has no dependency on Application, Infrastructure, or API.
2. Application has no dependency on Infrastructure, API, EF Core, Npgsql, ASP.NET Core, or Supabase.
3. Infrastructure has no dependency on API.
4. Controllers depend on Application only — not Infrastructure, repositories, `DbContext`, or domain services. The API composition root is the explicit exception for Infrastructure registration.
5. Repository and `IAIProvider` production implementations exist only in Infrastructure (test fakes excluded). Both halves are active: Phase 1 declared `IStudyItemRepository`/`IScoringConfigRepository`/etc.; `AnthropicAIProvider` (ADR-0019) is `IAIProvider`'s real implementation.

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
├── backend/                           ← ASP.NET Core solution — not the frontend
│   ├── CommitAhead.slnx
│   ├── global.json
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
├── docker-compose.e2e.yml             ← isolated E2E stack; only `proxy` is host-facing
└── e2e/                               ← Playwright suite — foundation implemented, journeys not
    ├── playwright.config.ts             written yet; own package.json, never in the app
    ├── scripts/                          dependency tree
    │   run-full.mjs (lifecycle), reset-db.mjs (only reset path), verify-foundation.mjs
    ├── support/                       ← reset.sql, db-init/ (roles→bundle→RLS), external-stub/
    └── tests/                         ← fixtures/e2e-test.ts, journeys/001–004
```

`frontend/dist` is never committed and never copied into `backend/src`. It is copied into the
published backend artifact's `wwwroot` only during `dotnet publish` (see the
`CopyFrontendBuildToPublishOutput` MSBuild target in `CommitAhead.Api.csproj`).

## Frontend design contract

- The approved identity is **Reading Room** with the **Bookmark** mark. The canonical design
  documentation is `docs/design/design-system/readme.md`.
- Before frontend work, read that document plus `components.md` and `page-patterns.md` in the same
  directory. `CONTEXT.md` and the domain/ADR documents remain authoritative for behaviour and
  terminology whenever a visual reference disagrees with them.
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
  the CSP in `docs/security/threat-model.md`. Values computed by the backend, including
  EffectiveScore, Demand and Mastery, are rendered from API responses and never recomputed in
  React.

## E2E testing contract

Before creating or changing anything under `e2e/`, the Playwright config, or the E2E Docker stack,
read **`docs/testing/strategy.md` Layer 7** (the normative contract — journeys, environment
isolation, E2E-only auth, locators, external-call rules) and **`e2e/README.md`** (the operational
runbook). Layer 7 wins if the two ever disagree. Its non-negotiables: exactly four journeys, each
passing independently and in any order (numeric filename prefixes are organizational, never
load-bearing); a fully isolated and non-persistent E2E stack that never touches the dev or
local-production database; E2E-only authentication that fails closed outside the `E2E` environment,
minted per journey by a test-scoped in-memory fixture that runs after the database reset — no setup
project and no `storageState` file; `workers: 1`; user-facing locators (role, label, text) with
`data-testid` only as a documented last resort where no meaningful accessible locator exists; no
CSS/XPath and no `waitForTimeout`; zero real Supabase Storage calls and no real AI provider
call — `external-stub` above is how E2E satisfies that, not a loophole in it.

**Only `proxy` is host-facing.** `app`, `db`, `db-init`, and `external-stub` sit on an
`internal: true` Compose network with no route off it — verified empirically, not merely
configured (an internal-only service's `ports:` entry is silently ignored). `proxy` is a plain
nginx reverse proxy dual-homed onto that network and an ordinary bridge network, forwarding only
to `app`; `db-init` runs roles → EF migration bundle → RLS once and exits, and `app` starts only on
its `service_completed_successfully`.

The canonical layout and per-file ownership are fixed in strategy.md §7.11 — `docker-compose.e2e.yml`
owns the topology, `playwright.config.ts` owns execution config only, `tests/fixtures/e2e-test.ts`
owns reset-then-authenticate (via the lazy `e2eSession`/`authenticatedPage` fixtures — the built-in
`page` fixture is never overridden), `support/reset.sql` owns the deterministic SQL alone (never
dropping migrations or RLS), `scripts/run-full.mjs` owns the stack lifecycle with a best-effort
`down -v` on success, failure, and `SIGINT`/`SIGTERM` alike, and `tests/journeys/` holds exactly the
four approved journeys. Do not invent folders outside it.

**Exactly one executable reset path: `e2e/scripts/reset-db.mjs`.** It validates the running
container's own Compose-project label (`commitahead-e2e`) and the database name
(`commitahead_e2e`) before piping `reset.sql` to `psql` over stdin, connected as
`commitahead_migrator` (the table owner). It exports `resetDatabase()` for the fixture and runs
from the CLI as `npm run db:reset`. `run-full.mjs` delegates to it. Never add a separate
Compose/`psql` reset in a fixture, script, or doc.

Also: ordinary PRs do not execute Playwright, and the E2E stack is started only for explicit E2E
work. `@playwright/test` is the permanent suite; Playwright's Agent CLI is an optional local
exploration aid whose output is never committed as generated and never a CI dependency. The
`/devalente-e2e` skill (`.claude/skills/devalente-e2e/`) is project-specific and version-controlled
— `.gitignore` has a narrow negation for that one directory while the rest of `.claude/` stays
ignored — but it **must not be created until the E2E suite is implemented and stable**.

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
- **Zero real AI calls** — `FakeAIProvider` enforced (PR gates cover layers 1–6 only; Layer 7's
  local-AI-stub exception is post-merge)

**Tests:**
- Domain unit tests
- Application use-case tests (handwritten fakes)
- Repository / integration tests (Testcontainers PostgreSql + Respawn, serial) — includes applying `001_roles.sql`/`002_rls_users.sql`/`003_rls_phase1.sql`/`004_rls_phase2.sql` against a disposable database and proving RLS isolation end to end (`RlsIsolationTests`, `RlsIsolationPhase2Tests`, `RlsHttpIsolationTests`); those scripts are never run by the production application itself, but CI does run them
- API tests (WebApplicationFactory + shared Testcontainers DB + `FakeAIProvider`)
- NetArchTest architecture rules
- Security API tests (auth, CSRF, CSP, CORS, `Cache-Control: no-store`, malicious uploads, AI schema validation, idempotency, rate/budget limits, log redaction)
- Frontend/export security tests for restricted Markdown rendering and dangerous-link protocols
- Parsed PDF/CV assertions

**Post-merge / manual only:**
- Playwright E2E (4 journeys) — still deferred; adding the project and writing the journeys is tracked as its own item in `docs/roadmap.md`, not assumed done
- Visual regression fixtures (per CV template)
- SBOM generation + Trivy container scan (high/critical blocks deployment)
- OWASP ZAP baseline (FakeAIProvider, fail on confirmed high-severity)
- Live AI smoke tests (manual trigger, explicit cost ceiling, never scheduled)
