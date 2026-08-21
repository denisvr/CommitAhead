# CommitAhead — Engineering Standards Adoption Plan

Analysis date: 2026-08-20. Canonical contract analysed: the `engineering-standards` repository
(`ENGINEERING.md` plus the routed detailed standards listed in section 3).

This document is the **analysis and migration plan**. Sections 1 to 7 remain a snapshot of the
repository as analysed on the date above; section 8 is kept current as phases land.
Phases 0 and 1 are complete and Phase 2 is next — `docs/current-state.md` is the authority on status. Section 10
records the decisions this analysis raised and how each was resolved; `docs/tbd.md` remains the owner
of unresolved project decisions.

Nothing in this document overrides an accepted ADR. The reversals it required are recorded as ADRs:
ADR-0025 (the standards are the canonical contract), ADR-0026 (CQRS vertical slices with a
project-owned mediator, superseding ADR-0008), ADR-0027 (security profile S2), and ADR-0028
(transaction and RLS ownership).

---

## 1. Executive summary

CommitAhead is a well-engineered, well-documented modular monolith that diverges from the canonical
engineering contract in one large, coherent way: it deliberately chose feature-folder *use-case
classes* over CQRS vertical slices with mediator dispatch (ADR-0008), and *repository ports* over a
direct EF Core DbContext boundary. Everything downstream of that choice — controller shape,
expected-failure modelling, HTTP error contract, and test structure — follows from it.

What is already strong: Clean Architecture layering with enforced dependency direction (NetArchTest,
5 rules), `net10.0` everywhere with a pinned SDK, MVC-only HTTP surface, domain purity (private
setters, read-only collections, no EF attributes), per-entity `IEntityTypeConfiguration`,
Testcontainers + Respawn with a pinned Postgres image, real RLS cross-owner isolation tests,
default-deny authorization with an enabled-user requirement, CSRF/headers/rate-limit tests,
committed lock files, SHA-pinned Actions, Dependabot, Gitleaks, and a CI generated-client drift
check. Several of these exceed the baseline.

What must change, in order of weight:

1. **The canonical contract is not connected.** No `docs/engineering-context.md`; `CLAUDE.md` and
   `AGENTS.md` restate project policy instead of importing `ENGINEERING.md`, and ADR-0008 directly
   contradicts a non-negotiable rule.
2. **No CQRS/vertical slices and no `IApplicationMediator`** — 28 `*UseCase` classes with
   `ExecuteAsync`, injected directly into two multi-operation controllers (10 and 13 actions).
3. **Repository ports wrap ordinary EF Core work**, including `SaveChangesAsync` on the port — the
   exact shape the standard names as prohibited.
4. **No shared Result/Error contract and no RFC 9457 Problem Details.** Expected failures are
   `null`, enums, and a `DomainValidationException` → 422 exception filter; there are **zero**
   `ProducesResponseType` declarations in the whole backend.
5. **No error localization**, and the export endpoint's binary body is absent from the generated
   client contract (`content?: never`), worked around with `parseAs: 'blob'`.

No persisted-data migration is required by any of this. The observable HTTP contract changes only in
the error-body and OpenAPI-declaration phases. The single genuinely hard technical decision is who
owns the transaction once the shared EF Core command behavior meets the existing RLS `set_config`
transaction (section 10 B).

Exact scale: **28 application operations** become 28 slices and 28 endpoint classes — two in the
Phase 3 pilot, the remaining **26** in Phase 4 — alongside 3 repositories deleted and 2
multi-operation controllers dissolved. Section 8 carries the operation-by-operation inventory.
Eleven phases, each independently buildable and revertable.

---

## 2. Current architecture map

**Topology:** single deployable ASP.NET Core 10 host serving the React SPA from `wwwroot` (copied at
`dotnet publish` only) — a modular monolith, not a BFF-plus-service split.

```text
backend/CommitAhead.slnx
  src/CommitAhead.Domain          -> (no project refs)
  src/CommitAhead.Application     -> Domain             [Markdig, DI.Abstractions, Logging.Abstractions]
  src/CommitAhead.Infrastructure  -> Domain, Application [EF Core 10, Npgsql 10, QuestPDF, PdfPig, Http]
  src/CommitAhead.Api             -> Application, Infrastructure [JwtBearer, AspNetCore.OpenApi]
  tests/{Domain,Application,Infrastructure,Api}.Tests
frontend/  React 19 + Vite 8 + TS 6, openapi-typescript + openapi-fetch, CSS Modules
e2e/       Playwright, 2 approved journeys, own package.json (out of scope here)
```

| Area | Current implementation | Evidence |
|---|---|---|
| Dependency direction | Correct; API references Infrastructure only at composition root (ADR-0013) | `backend/tests/CommitAhead.Api.Tests/Architecture/ArchitectureTests.cs` |
| Use cases | 28 `*UseCase` classes, `ExecuteAsync`, no command/query split, no validators, no `Features/` folder | `backend/src/CommitAhead.Application/ProfessionalProfiles/ReplaceExperienceUseCase.cs` |
| Dispatch | None — constructor-injected use cases (14 in one controller) | `backend/src/CommitAhead.Api/Features/CVPresentations/CVPresentationController.cs:26` |
| Endpoints | 2 multi-operation controllers (10 + 13 actions), 6 auth controllers, health, me, 2 catch-alls; `MapFallbackToFile` for the SPA shell only | `backend/src/CommitAhead.Api/Features/ProfessionalProfiles/ProfessionalProfileController.cs` |
| Persistence | `CommitAheadDbContext` (Infrastructure) behind `IProfessionalProfileRepository`, `ICVPresentationRepository`, `IUserRepository`; ports expose `SaveChangesAsync` | `backend/src/CommitAhead.Infrastructure/ProfessionalProfiles/ProfessionalProfileRepository.cs` |
| Transactions / tenancy | `RlsTransactionActionFilter` opens a transaction + `set_config('app.current_user_id', …, is_local)` per `[UsesOwnerScopedData]` action; RLS policies in `scripts/database/001,002,004` | `backend/src/CommitAhead.Infrastructure/Persistence/RlsSessionContext.cs` |
| Migrations | 13 EF migrations + model snapshot, Postgres | `backend/src/CommitAhead.Infrastructure/Persistence/Migrations/` |
| Expected failures | `null`, `ProfessionalProfileMutationResult`/`CVPresentationMutationResult` enums, `ExportCVPresentationOutcome`, `DomainValidationException` → 422 | `backend/src/CommitAhead.Api/Filters/ValidationExceptionFilter.cs` |
| Error contract | Bare `NotFound()`/`Conflict()`/`UnprocessableEntity()`; one bespoke `ProblemDetails` with an `outcomeCode` extension; 0 `ProducesResponseType` | `backend/src/CommitAhead.Api/Features/CVPresentations/CVPresentationController.cs:174` |
| OpenAPI / client | `Microsoft.AspNetCore.OpenApi` build-time document → `openapi-typescript` `schema.d.ts` + `openapi-fetch`; CI drift check present | `.github/workflows/ci.yml`, `frontend/package.json` |
| Frontend API layer | `api/client.ts` (single-flight refresh + 401 retry middleware) + 2 feature `api.ts` adapters; errors flattened to one string via 38 `describeError` sites; no localization | `frontend/src/api/client.ts`, `frontend/src/features/professional-profile/api.ts` |
| Auth | Backend-mediated Supabase magic-link/PKCE, HttpOnly cookies, JWT via cookie, 15-min `iat` ceiling, `FallbackPolicy` + `DefaultPolicy` = authenticated + `EnabledUserRequirement`, antiforgery double-submit, per-IP login limiter | `backend/src/CommitAhead.Api/DependencyInjection/AuthenticationServiceCollectionExtensions.cs` |
| Secrets/config | User secrets + env; options bound **without** `ValidateOnStart` (build-time OpenAPI generation runs the host); `E2EConfigurationGuard` fails closed for the E2E environment | `backend/src/CommitAhead.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs:31` |
| Tests | 4 projects; unit + Testcontainers + NetArchTest mixed inside `Infrastructure.Tests` and `Api.Tests`; `postgres:17-alpine` pinned; Respawn | `backend/tests/**` |
| Shared packages | **None consumed**; no `NuGet.Config`, no private feed | — |

**Classification used for standards routing:** modular monolith · MVC API + React SPA · relational
persistence (EF Core 10 / Npgsql) · frontend client generation present · one outbound external
integration (Supabase Auth, typed `IHttpClientFactory` client) · write use cases present ·
Testcontainers integration tests already present · **security profile S2** (authenticates users,
mutates durable personal data, internet-intended).

---

## 3. Applicable standards read (complete)

`ENGINEERING.md` · `docs/architecture/README.md` · `foundations.md` ·
`cqrs-and-vertical-slices.md` · `modular-monolith.md` · `reference-structure.md` ·
`results-and-errors.md` · `error-localization.md` · `frontend.md` · `api-clients.md` ·
`docs/development/adopting-the-standards.md` · `project-naming.md` · `csharp-style.md` ·
`docs/testing/README.md` · `integration-tests.md` · `docs/security/README.md` ·
`security-profiles.md` · `secure-coding.md` · `authentication-and-authorization.md` ·
`api-and-bff.md` · `security/frontend.md` · `data-and-integrations.md` ·
`secrets-and-cryptography.md` · `supply-chain.md` · `threat-modeling.md` ·
`ai-assisted-development.md` · `verification.md` · `docs/shared/README.md` ·
`docs/decisions/README.md`.

Project side: `CLAUDE.md`, `AGENTS.md`, `CONTEXT.md`, `docs/current-state.md`,
`docs/architecture/solution.md`, `docs/testing/strategy.md`, `docs/security/threat-model.md`,
`docs/roadmap.md`, `docs/tbd.md`, ADR-0008, ADR-0013.

## 4. Standards intentionally not loaded

| Not loaded | Why |
|---|---|
| `architecture/distributed-services.md` | One deployable host; no independently released service, no extraction planned. |
| `architecture/backend-for-frontend.md` | No client-specific aggregation boundary over downstream APIs. The BFF *browser-session* rules from `authentication-and-authorization.md` were read and do apply — they are already satisfied. |
| C# downstream client generation (`api-clients.md`, "C# clients") | Read for completeness but not applicable: no external .NET consumer; the one outbound boundary (GoTrue) is a hand-typed `IHttpClientFactory` client, which the standard permits. |
| `security/operations-and-incident-response.md` | Owns deployed logging/alerting/response; Phase 6c internet deployment is explicitly deferred (`docs/current-state.md`) and unauthorized to start. |
| `security/security-plan-template.md` | A deliverable template, not an input; instantiated in Phase 11, not needed to find gaps. |
| `development/branching.md` | Source-control workflow, outside the requested migration scope. |
| Browser E2E standards | None exist yet ("a future increment"), and E2E adoption is not part of this migration. The project's own `docs/testing/strategy.md` Layer 7 stays authoritative. |
| Messaging, caching, observability packages | No broker, cache, or telemetry stack exists or is planned. |
| Auditing/soft-delete/purge material | Read inside `foundations.md`/`cqrs-and-vertical-slices.md`; not applicable — no entity currently requires audit stamps beyond its own `CreatedAtUtc`/`UpdatedAtUtc`, and deletion is hard-delete by design. |

## 5. Standards already satisfied

- **Clean Architecture foundation and dependency direction**, enforced by tests, with the API
  composition root as the documented Infrastructure exception (ADR-0013 aligns with
  `foundations.md`).
- **`net10.0` baseline** across all 8 projects; `global.json` pins SDK `10.0.302`;
  `TreatWarningsAsErrors`, `AnalysisLevel=latest`, `RestorePackagesWithLockFile`.
- **MVC Controllers only** — no Minimal API application endpoints. `MapFallbackToFile("index.html")`
  is static SPA shell serving, not an HTTP API operation.
- **Domain purity**: no EF/ASP.NET types, private setters, read-only collection views, invariants in
  aggregates, one type per domain file.
- **Infrastructure ownership** of provider details: `IEntityTypeConfiguration` per entity,
  migrations, value converters, QuestPDF renderer behind an Application `IExportRenderer` capability
  port.
- **Contracts do not leak EF entities**; API owns `*Request`/`*Response` records and maps explicitly
  (manual mapping, no reflection mapper).
- **Cancellation tokens** threaded through every async boundary; no `.Result`/`.Wait()`.
- **`IHttpClientFactory`** typed client for the one outbound integration; no manual `HttpClient`.
- **Default-deny authorization**: `FallbackPolicy` *and* `DefaultPolicy` both require authenticated +
  enabled user; `[AllowAnonymous]` confined to health, auth, and the two 404 catch-alls, each with
  tests.
- **BFF-style browser session**: tokens server-side only in HttpOnly/Secure cookies, never in
  `localStorage`; antiforgery on unsafe cookie-authenticated methods; logout revokes at Supabase,
  not just the cookie; 15-minute server-side `iat` ceiling; JWKS retrieval hardened against a
  self-reported `jwks_uri`.
- **Structured logging discipline**: exception *type* only on rollback failure, no object/message
  dumps; log-redaction tests exist.
- **Secure coding specifics**: parameterized `ExecuteSqlInterpolated`, restricted Markdown parser +
  dangerous-protocol link tests, CSP without `unsafe-inline` (documented as constraining the drag
  implementation), no `ExecuteDeleteAsync`/`IgnoreQueryFilters` anywhere.
- **Testcontainers standard**: `postgres:17-alpine` pinned (never `latest`), fixture lifecycle,
  Respawn reset with migration-history exclusion, RLS scripts applied against a disposable database,
  cross-owner isolation proven end to end, no real third-party calls in tests.
- **Supply chain**: committed `packages.lock.json` x8, `--locked-mode` restore,
  `dotnet list package --vulnerable --include-transitive` failing on any hit,
  `npm audit --audit-level=high`, Gitleaks, SHA-pinned Actions, Dependabot,
  `permissions: contents: read`, documented reason for each npm `override`.
- **Generated-client discipline**: generated files never hand-edited, regenerated in CI with a
  `git diff --exit-code` drift gate, contract derived from the built project (not a live endpoint).
- **ADR practice**: 24 ADRs with status/context/decision/consequences/alternatives and explicit
  supersession — matches `decisions/README.md`.

## 6. Gap table

Severity: **M** mandatory violation · **S** security · **A** architectural migration ·
**C** consistency · **O** optional. Type: mechanical / architectural / behavioral / security.
"Contract" = changes a public API or persisted data.

| # | Sev | Gap (current → target) | Evidence | Type | Contract | Risk | Depends on |
|---|---|---|---|---|---|---|---|
| 1 | M | Canonical contract not connected: no `docs/engineering-context.md`; `CLAUDE.md`/`AGENTS.md` duplicate policy instead of importing `ENGINEERING.md` → create context file, convert both to adapters | `CLAUDE.md`, `AGENTS.md` | mechanical | no | none | — |
| 2 | M | ADR-0008 forbids exactly what the contract mandates (mediator dispatch, canonical CQRS names) → supersede with a CQRS + `IApplicationMediator` ADR | `docs/adr/0008-feature-folder-use-cases-without-mediatr.md` | architectural | no | low | 1 |
| 3 | M/A | 28 `*UseCase.ExecuteAsync` classes, no command/query split, no `Features/<Feature>/{Commands,Queries}/<Operation>/`, no validators → canonical slices with `<Op>Command/Query`, `…Handler`, `…Result`, `<Op>Validator` | `backend/src/CommitAhead.Application/{ProfessionalProfiles,CVPresentations,Auth,Identity}/*.cs` | architectural | no | **high** (volume) | 2, 5 |
| 4 | M/A | No dispatch boundary; controllers inject up to 14 use cases → `IApplicationMediator.SendAsync` from every endpoint; `AddDevalenteMediator(ApplicationAssembly)` | `CVPresentationController.cs:26-54` | architectural | no | medium | 3, 13 |
| 5 | M/A | 3 repository ports around ordinary EF Core, including `SaveChangesAsync` on the port → one `ICommitAheadDbContext` in Application used directly by handlers; delete the repositories; `AsNoTracking()` + projections on reads | `IProfessionalProfileRepository.cs`, `ICVPresentationRepository.cs`, `IUserRepository.cs` | architectural | no | **high** | 13 |
| 6 | M | NetArchTest rule 2 forbids `Microsoft.EntityFrameworkCore` in Application, which blocks the mandated DbContext boundary → relax to allow provider-neutral EF Core while still banning Npgsql/Infrastructure/ASP.NET; rewrite rule 5 (`PersistencePorts`) | `ArchitectureTests.cs:29-42,106` | architectural | no | low | 5 |
| 7 | M/A | Multi-operation controllers (10 and 13 actions) → one `<Op>Endpoint` class + one action each, under an abstract base endpoint owning the route prefix; routes stay byte-identical | both feature controllers | architectural | no | medium | 4 |
| 8 | M | No `Result`/`Result<T>`/`Error`; failures are `null`, enums, and `DomainValidationException` used as control flow → shared Results + per-feature error catalogs with stable lowercase codes | `ProfessionalProfileMutationResult.cs`, `ValidationExceptionFilter.cs` | behavioral | no | medium | 3, 13 |
| 9 | M | No RFC 9457 contract; bespoke `outcomeCode` extension; **zero** `ProducesResponseType` → `ApiProblemDetails` + `errors[]` via injected `IResultProblemDetailsFactory`, declared statuses per operation | grep: 0 hits in `backend/src/` | behavioral | **yes** | medium | 8 |
| 10 | M | No input validation layer; ad-hoc guards inline (e.g. email regex in the controller) → FluentValidation validators + validation behavior registered before the transaction behavior | `LoginController.cs:31-38` | behavioral | **yes** (400 bodies) | medium | 8 |
| 11 | M | Export operation advertises no body (`content?: never`) and no failure statuses; client uses `parseAs:'blob'` → declare `application/pdf` binary success + `application/problem+json` failures; add OpenAPI-shape integration tests | `frontend/src/api/generated/schema.d.ts:888-896`, `frontend/src/features/cv-presentations/api.ts:163` | behavioral | **yes** | low | 9 |
| 12 | M | Generator is `openapi-typescript`, not NSwag, with no ADR recording the retention exception → migrate to NSwag **or** write the ADR (the standard permits the latter) | `frontend/package.json` | architectural | yes (if migrated) | medium | 9 |
| 13 | M | `Devalente.Shared.*` not consumed; no `NuGet.Config`/private feed → configure the credential-free source and install Cqrs, Results, Validation.FluentValidation, AspNetCore.Mvc, EntityFrameworkCore, Security.Testing (+ OpenApi.NSwag if 12 migrates) | no `nuget.config` found | mechanical | no | **blocking** (feed access) | — |
| 14 | M | No error localization: no `src/localization/`, 38 `describeError` sites flattening errors to one string; hard-coded English | `frontend/src/features/*/api.ts` | behavioral | no | medium | 9 |
| 15 | M | Multiple top-level types per file across API and Application → one type per file, named after the type | `ProfessionalProfileController.cs` (5 types), `ProfessionalProfileEntryDtos.cs` (catch-all), `CVPresentationController.cs` (5), `GetProfessionalProfileUseCase.cs` (2), `CVExportDocument.cs`, `HealthController.cs`, `MeController.cs`, `LoginController.cs`, `CsrfController.cs` | mechanical | no | none | fold into 3/7 |
| 16 | M/S | Protected actions carry no explicit `[Authorize]` (they rely solely on `FallbackPolicy`); no mechanical MVC authorization-inventory test → explicit attribute on every operation + `MvcEndpointAuthorizationVerifier` with a reviewed `ApprovedAnonymousEndpoints` list | `MeController.cs:6-14` comment; no `IActionDescriptorCollectionProvider` in tests | security | no | low | 13 |
| 17 | S | No recorded security profile / ASVS tailoring / evidence register; `threat-model.md` has no ASVS identifiers → record **S2** in an ADR + evidence table | `docs/security/threat-model.md` | security | no | none | — |
| 18 | S | Options bound **without** `ValidateOnStart` by design (build-time OpenAPI generation runs the host) → fail-closed startup validation; NSwag `noBuild` or a design-time guard removes the reason | `InfrastructureServiceCollectionExtensions.cs:31-40`, `AuthenticationServiceCollectionExtensions.cs:120` | security | no | medium (startup) | 12 |
| 19 | S | Rate limits only on `/auth/login`; the CPU-heavy PDF export and all write endpoints are unlimited; no HTTP-level body/collection/pagination ceilings (domain `ValidationLimits` is not a transport limit) → finite limits + a policy on export | `SecurityServiceCollectionExtensions.cs:31-45`; export action comment | security | no | low | — |
| 20 | S | `AnalysisLevel=latest` but security rules are not raised to error-all → `latest-all` (or the Security category explicitly as errors), per `secure-coding.md` "Static verification" | `backend/Directory.Build.props` | security | no | low (new warnings) | — |
| 21 | C | Test projects mix unit + Testcontainers + architecture tests → `Infrastructure.IntegrationTests` / `Api.IntegrationTests` with `Fixtures/` + `Features/`, unit tests separated, split CI jobs | `Infrastructure.Tests`, `Api.Tests` | mechanical | no | low | — |
| 22 | C | Authorization matrix incomplete: no per-operation "another owner's resource → 404/403" HTTP tests (owner isolation is proven at the RLS/repository layer instead) | `Api.Tests/{CVPresentations,ProfessionalProfiles}` | security | no | low | 7 |
| 23 | C | Cross-aggregate `DanglingSelectionCleanup` static helper called from 7 use cases → an application service owned by the feature, invoked from the handlers | `DanglingSelectionCleanup.cs` | architectural | no | medium | 3 |
| 24 | C | Stale `InternalsVisibleTo` comment referencing removed AI parsers | `backend/src/CommitAhead.Application/CommitAhead.Application.csproj` | mechanical | no | none | — |
| 25 | C | Docs describe the pre-migration architecture (`solution.md` "Repository interfaces", `CLAUDE.md` "no MediatR / one use case class per operation", `testing/strategy.md` layer names) | `docs/architecture/solution.md`, `CLAUDE.md` | mechanical | no | none | all |
| 26 | O | Deletion policy documented only in a `DeleteCVPresentationUseCase` docstring; `ProfessionalProfile`/`User` have no delete path → state the hard-delete policy per entity explicitly | that docstring | mechanical | no | none | — |
| 27 | O | Hand-rolled `CreatedAtUtc`/`UpdatedAtUtc` instead of `Devalente.Shared.Auditing.Abstractions` (opt-in; not required) | domain aggregates | architectural | yes (columns) | medium | — |
| 28 | O | `Markdig` + `RestrictedMarkdownParser` live in Application (a pure library, permitted, but worth an explicit note in the context file) | Application `.csproj` | — | no | none | — |

**Not applicable / no gap:** soft delete, purge, `IgnoreQueryFilters`, raw-SQL feature ports (the one
raw statement is the RLS `set_config`, correctly in Infrastructure), generic repository removal
beyond #5, downstream C# ApiClient package, messaging, observability, multi-module project split,
namespace changes (`CommitAhead.*` is consistent and must be preserved).

**Explicitly not a violation — the `E2ESessionController`.** The standards prohibit *reachable* test
authentication in production. Here `E2EConfigurationGuard.Validate` fails closed before the pipeline
is built, the endpoint 404s outside `ASPNETCORE_ENVIRONMENT=E2E`, it is absent from the generated
OpenAPI document, and all of that is asserted by tests. It stays, and its tests remain mandatory.

---

## 7. Proposed target architecture

```text
backend/src/CommitAhead.Application/
  Data/ICommitAheadDbContext.cs                  <- DbSet<User|ProfessionalProfile|CVPresentation>
  Features/
    ProfessionalProfiles/
      Commands/ReplaceExperience/
        ReplaceExperienceCommand.cs
        ReplaceExperienceCommandHandler.cs        <- ICommandHandler<…, Result>
        ReplaceExperienceValidator.cs
      Queries/GetProfessionalProfile/
        GetProfessionalProfileQuery.cs
        GetProfessionalProfileQueryHandler.cs     <- IQueryHandler<…, Result<…QueryResult>>
        GetProfessionalProfileQueryResult.cs
      ProfessionalProfileErrors.cs                <- professional_profile.not_found, …
    CVPresentations/ { Commands/…, Queries/…, CVPresentationErrors.cs }
    Auth/ { Commands/Login, Callback, Refresh, Logout }
    Identity/Queries/GetCurrentUser/
  Storage/ (unchanged capability ports: IExportRenderer, ISupabaseAuthClient, IRlsSessionContext)

backend/src/CommitAhead.Api/Features/
  ProfessionalProfiles/
    ProfessionalProfilesBaseEndpoint.cs           <- [ApiController][Authorize][Route("api/professional-profile")]
    ReplaceExperience/{ReplaceExperienceEndpoint,ReplaceExperienceRequest}.cs
    GetProfessionalProfile/{…Endpoint,…Response}.cs
  CVPresentations/ …  (incl. ExportCVPresentation -> application/pdf declared)
  Auth/ …   Health/HealthEndpoint.cs   Me/GetCurrentUserEndpoint.cs   Routing/ (unchanged)

backend/src/CommitAhead.Infrastructure/Persistence/CommitAheadDbContext.cs : DbContext, ICommitAheadDbContext
  (repositories deleted; configurations, migrations, RlsSessionContext unchanged)

backend/tests/
  CommitAhead.Domain.Tests/ · CommitAhead.Application.Tests/
  CommitAhead.Infrastructure.IntegrationTests/{Fixtures,Features}
  CommitAhead.Api.IntegrationTests/{Fixtures,Features}  <- + MvcEndpointAuthorizationVerifier
  CommitAhead.Architecture.Tests/   (or kept inside Api.IntegrationTests)

frontend/src/
  api/{client.ts, generated/}        <- generated output unedited; problem-details preserved
  localization/{i18n.ts, locales/en/errors.json}
  features/**                        <- translateApiError(code, parameters) replaces describeError
```

Composition root order (`Program.cs`): controllers → `AddDevalenteMvcProblemDetails` → DbContext
(+ `ICommitAheadDbContext` as the same scoped instance) → capability ports → `AddDevalenteMediator`
→ `AddDevalenteRequestValidation` → `AddDevalenteEfCoreTransactions<CommitAheadDbContext>` → RLS
owner-scope behavior (see section 10 B).

Deliberately **not** added: no new projects beyond the test split, no BFF, no worker, no
messaging/cache/observability stack, no E2E framework changes, no `Devalente.Shared.Testing`.

---

## 8. Phased migration plan

Every phase leaves `dotnet build --warnaserror` and `npm run build` green, and ends with the
verification block in section 9.

**PR granularity.** Phases 0 and 1 shipped together as a single baseline pull request. That is the
one recorded exception, and it is deliberate: neither phase changes application behaviour beyond
adding limits, and splitting instruction-file plumbing from the dependency and analyzer baseline
would have produced two PRs that cannot be reviewed independently anyway. Every architectural phase
from Phase 2 onward is its own reviewed pull request, and Phase 4 is further split one pull request
per feature folder. Do not fold a later phase into an earlier one.

**Package installation is per phase.** A `Devalente.Shared.*` reference is added in the phase whose
code uses it, never earlier. An unused package reference is unreviewable: nothing in the diff shows
what it is for.

| Package | Introduced in |
|---|---|
| `Devalente.Shared.AspNetCore.Security.Testing` | Phase 1 (endpoint-authorization inventory) |
| `Devalente.Shared.Cqrs.Abstractions`, `Devalente.Shared.Cqrs` | Phase 3 (mediator dispatch) |
| `Devalente.Shared.Results` | Phase 3 (pilot command and query contracts) |
| `Devalente.Shared.Validation.FluentValidation` and `FluentValidation` | Phase 3 (pilot validator) |
| `Devalente.Shared.EntityFrameworkCore` | Phase 3 (command transaction behaviour) |
| `Devalente.Shared.AspNetCore.Mvc` | Phase 3 (pilot Problem Details responses) |
| `Devalente.Shared.OpenApi.NSwag` | Phase 8 (generator migration) |
| `Devalente.Shared.Auditing.Abstractions` | Not planned (ADR decision H: skipped) |

### Phase 0 — Adoption metadata and tool routing (complete)

Created `docs/engineering-context.md`; converted `CLAUDE.md` and `AGENTS.md` into discovery adapters
importing `../engineering-standards/ENGINEERING.md` plus the context file; recorded ADR-0025
(canonical contract), ADR-0026 (CQRS slices with a project-owned mediator, superseding ADR-0008),
ADR-0027 (security profile S2), and ADR-0028 (transaction and RLS ownership).

Documentation only; no behaviour change.

### Phase 1 — Baseline build, dependency, and security controls (complete)

- `NuGet.Config` declaring the private feed with no credential, and `packageSourceMapping` pinning
  the `Devalente.Shared.*` prefix to it.
- CI authenticates that feed in both jobs that restore .NET projects (`backend`,
  `combined-artifact`), failing closed with a named error when the secret is absent, and
  `packages: read` is granted to exactly those two jobs.
- Dependabot private registry configured against its own separate secret store.
- Explicit `[Authorize]` on every protected MVC operation, plus the mechanical
  endpoint-authorization inventory test and its reviewed `ApprovedAnonymousEndpoints` list.
- `AnalysisLevelSecurity=latest-all`; the one finding it surfaced fixed with a narrow justified
  suppression, not a global one.
- Finite transport ceilings: a Kestrel request-body cap derived from the domain's own limits, and a
  JSON depth bound on both serializer configurations.
- A global state-changing rate limit per authenticated caller, and a tighter CV-export policy, with
  enforcement tests including `429` and per-caller partition isolation.
- The S2 evidence register in `docs/security/threat-model.md`.

Only `Devalente.Shared.AspNetCore.Security.Testing` is installed in this phase.

**Not satisfied by code, and recorded as open in the evidence register:** the Actions secret, the
Dependabot secret, the package read grant, and branch protection are GitHub settings the repository
owner must configure. Kestrel's body limit is asserted as configuration, because a test host does not
run Kestrel; its runtime rejection is a deployed check.

### Phase 2 — Solution boundaries and dependency direction (next)

Split tests: `Infrastructure.Tests` becomes `Infrastructure.IntegrationTests` with unit tests kept
at the boundary they prove; `Api.Tests` becomes `Api.IntegrationTests`; decide where the architecture
tests live. Adopt the `Fixtures/` and `Features/` layout. Split CI into unit and integration jobs.

Do the mechanical one-type-per-file work only for files the later phases do not rewrite anyway
(`ProfessionalProfileEntryDtos.cs`, `CVExportDocument.cs`, the small controllers' response records).

**Done when:** `dotnet test` passes with identical test counts, and no formatting-only churn is left
to collide with a later architectural diff.

### Phase 3 — The pilot slice, end to end

This phase is deliberately self-contained: it stands up the complete dispatch pipeline for two
operations rather than deferring parts of it. A pilot that cannot execute its own validator or return
its own Problem Details would prove nothing about the target architecture.

Install and register, in this order:

1. `ICommitAheadDbContext` in Application, with `CommitAheadDbContext` and the interface resolving to
   the same scoped instance; the two NetArchTest rules relaxed to permit provider-neutral EF Core in
   Application while still banning Npgsql, Infrastructure, and ASP.NET Core.
2. `AddDevalenteMediator(ApplicationAssembly)` — the pilot endpoints dispatch exactly one command or
   query each and inject `IApplicationMediator`, nothing else.
3. `AddDevalenteRequestValidation(ApplicationAssembly)` — registered **before** the transaction
   behaviour, so an invalid request is refused before a transaction begins.
4. `AddDevalenteEfCoreTransactions<CommitAheadDbContext>` — the command transaction owner.
5. The RLS owner-scope behaviour, registered **after** the EF transaction behaviour so it runs
   inside the transaction that behaviour opened (ADR-0028).
6. `AddDevalenteMvcProblemDetails`, so the pilot's own failures return the canonical RFC 9457 shape
   through an injected `IResultProblemDetailsFactory`.

Migrate two operations: `GetProfessionalProfile` (query, `Result<T>`, `AsNoTracking` projection) and
`ReplaceExperience` (command, validator, one expected domain failure, `DanglingSelectionCleanup` as a
feature-owned service). Add `ProfessionalProfileErrors`, the two endpoint classes, and the
real-provider tests ADR-0028 requires. The three repositories stay for every operation not yet
migrated.

**Done when:** both pilot operations are green at unit, integration, and HTTP level; the pilot
validator demonstrably executes and its failures arrive as `ApiProblemDetails` with an `errors` array
and a JSON pointer; ADR-0028's verification gate passes in full; the old use-case classes for those
two operations are deleted. **Review before repeating** — this is the gate the adoption guide
requires, and the shape fixed here is the shape of everything in Phase 4.

### Phase 4 — Migrate the remaining 26 operations

Repeat the reviewed pilot pattern across the 26 operations listed in the inventory below, one pull
request per feature folder. Delete all three repositories and the `PersistencePorts` list once the
last operation that used them is migrated. `SaveChangesAsync` disappears from Application except
where the transaction behaviour owns it.

Routes, verbs, and success status codes stay byte-identical throughout, so the SPA, the generated
client, and both E2E journeys are unaffected by the restructure itself.

**Done when:** no `I*Repository` remains; every handler uses `ICommitAheadDbContext`; the RLS
isolation tests still prove cross-owner denial; both E2E journeys pass.

### Phase 5 — Prove no direct dispatch remains

Phase 4 removes the last direct call by construction, so this phase is a closing gate rather than new
plumbing, and it exists to stop the pattern coming back:

- delete the per-use-case registrations from `AddApplication`;
- add an architecture rule that an API endpoint may not depend on a handler, a use-case class, a
  repository, or a `DbContext` — the mechanical equivalent of the endpoint-authorization inventory,
  for dispatch;
- delete `RlsTransactionActionFilter` and `[UsesOwnerScopedData]`, whose responsibility moved into the
  pipeline in Phase 3.

**Done when:** the new architecture rule fails if a single endpoint reaches past the mediator.

### Phase 6 — Endpoint-per-operation restructure

Dissolve the two multi-operation controllers into abstract base endpoints plus one class and one
action each, with `<Op>Request` and `<Op>Response` in their own files. Routes, verbs, and success
status codes remain byte-identical.

**Done when:** `git diff` on `schema.d.ts` shows no path, verb, or success-shape change, and the API
tests are unchanged and green.

### Phase 7 — Complete the Result and RFC 9457 contract

Phase 3 already proved this path for the pilot; this phase generalizes it and removes the old
mechanisms. Complete the per-feature error catalogs; delete `ValidationExceptionFilter` and the
`DomainValidationException`-as-control-flow path, keeping constructor and value-object guards only
for genuine internal invariants; replace the bespoke `outcomeCode` extension and the bare
`NotFound()` and `Conflict()` results; declare `ProducesResponseType<ApiProblemDetails>` per
operation; convert the rate-limiter rejection body from plain text to the same contract.

**Done when:** every failure path returns `ApiProblemDetails` with an `errors` array, asserted on
exact code, type, status, and pointer.

### Phase 8 — OpenAPI and generated client

Adopt `Devalente.Shared.OpenApi.NSwag` with a committed, fully pinned `config.nswag`,
`DevalenteGenerateApiClients`, and `SkipNSwag=True` on integration-test project references; rewrite
the frontend API layer against the generated client. Fix the export operation's binary and
`problem+json` declarations, and add OpenAPI-shape assertions for binary and bodyless operations.
Enable `ValidateOnStart` once document generation no longer runs the host.

**Done when:** the CI drift check passes with the pinned tool, the client types the PDF response, and
startup fails closed on missing Supabase configuration, with a test.

### Phase 9 — Frontend error localization

Add `localization/i18n.ts` and `locales/en/errors.json`; normalize transport errors preserving
`code`, `detail`, `pointer`, `parameters`, and `traceId`; replace the 38 `describeError` sites with
`translateApiError`; associate pointer-bearing errors with their fields; add the CI key-set check.

**Done when:** no API error text is derived from a hand-parsed `message` or `detail` string, and the
fallback order (locale, default locale, `detail`, generic plus traceId) is tested.

### Phase 10 — Integration tests and Testcontainers completion

Fill the API authorization matrix per operation, add the Problem Details assertions, and run one
egress-disabled verification.

**Done when:** every protected operation has its matrix and the suite passes with outbound network
disabled.

### Phase 11 — Documentation reconciliation and final verification

Update `CLAUDE.md`, `AGENTS.md`, `docs/architecture/solution.md`, `docs/testing/strategy.md`,
`docs/security/threat-model.md`, `docs/current-state.md`, `docs/roadmap.md`, and `CONTEXT.md`;
finalize the ADR supersessions; confirm the recorded standards revision. Run the full suite plus both
E2E journeys.

**Done when:** no document describes repositories or use-case classes as current, and the evidence
register has an owner against every S2 control.

### Operation migration inventory

All 28 current `*UseCase` classes, each appearing exactly once. The pilot migrates two; Phase 4
migrates the remaining 26. Target names follow the canonical contract: `<Operation>Command` or
`<Operation>Query`, a matching `Handler`, and an `<Operation>Endpoint` owning one action.

Every route below is preserved byte-identical through Phases 3 to 6. The only contract changes are
the failure bodies (Phase 7) and the export operation's declared response (Phase 8).

| # | Current `*UseCase` | Route | Target request | Handler | Endpoint | Phase |
|---|---|---|---|---|---|---|
| 1 | `GetProfessionalProfileUseCase` | `GET /api/professional-profile` | `GetProfessionalProfileQuery` | `GetProfessionalProfileQueryHandler` | `GetProfessionalProfileEndpoint` | 3 |
| 2 | `ReplaceExperienceUseCase` | `PUT /api/professional-profile/experience` | `ReplaceExperienceCommand` | `ReplaceExperienceCommandHandler` | `ReplaceExperienceEndpoint` | 3 |
| 3 | `CreateProfessionalProfileUseCase` | `POST /api/professional-profile` | `CreateProfessionalProfileCommand` | `CreateProfessionalProfileCommandHandler` | `CreateProfessionalProfileEndpoint` | 4 |
| 4 | `UpdateProfessionalProfileUseCase` | `PUT /api/professional-profile` | `UpdateProfessionalProfileCommand` | `UpdateProfessionalProfileCommandHandler` | `UpdateProfessionalProfileEndpoint` | 4 |
| 5 | `ReplaceEducationUseCase` | `PUT /api/professional-profile/education` | `ReplaceEducationCommand` | `ReplaceEducationCommandHandler` | `ReplaceEducationEndpoint` | 4 |
| 6 | `ReplaceSkillsUseCase` | `PUT /api/professional-profile/skills` | `ReplaceSkillsCommand` | `ReplaceSkillsCommandHandler` | `ReplaceSkillsEndpoint` | 4 |
| 7 | `ReplaceLanguagesUseCase` | `PUT /api/professional-profile/languages` | `ReplaceLanguagesCommand` | `ReplaceLanguagesCommandHandler` | `ReplaceLanguagesEndpoint` | 4 |
| 8 | `ReplaceCertificationsUseCase` | `PUT /api/professional-profile/certifications` | `ReplaceCertificationsCommand` | `ReplaceCertificationsCommandHandler` | `ReplaceCertificationsEndpoint` | 4 |
| 9 | `ReplaceProjectsUseCase` | `PUT /api/professional-profile/projects` | `ReplaceProjectsCommand` | `ReplaceProjectsCommandHandler` | `ReplaceProjectsEndpoint` | 4 |
| 10 | `ReplaceProfileLinksUseCase` | `PUT /api/professional-profile/profile-links` | `ReplaceProfileLinksCommand` | `ReplaceProfileLinksCommandHandler` | `ReplaceProfileLinksEndpoint` | 4 |
| 11 | `GetCVPresentationsUseCase` | `GET /api/cv-presentations` | `GetCVPresentationsQuery` | `GetCVPresentationsQueryHandler` | `GetCVPresentationsEndpoint` | 4 |
| 12 | `GetCVPresentationUseCase` | `GET /api/cv-presentations/{id}` | `GetCVPresentationQuery` | `GetCVPresentationQueryHandler` | `GetCVPresentationEndpoint` | 4 |
| 13 | `CreateCVPresentationUseCase` | `POST /api/cv-presentations` | `CreateCVPresentationCommand` | `CreateCVPresentationCommandHandler` | `CreateCVPresentationEndpoint` | 4 |
| 14 | `UpdateCVPresentationUseCase` | `PUT /api/cv-presentations/{id}` | `UpdateCVPresentationCommand` | `UpdateCVPresentationCommandHandler` | `UpdateCVPresentationEndpoint` | 4 |
| 15 | `DeleteCVPresentationUseCase` | `DELETE /api/cv-presentations/{id}` | `DeleteCVPresentationCommand` | `DeleteCVPresentationCommandHandler` | `DeleteCVPresentationEndpoint` | 4 |
| 16 | `ReplaceExperienceSelectionsUseCase` | `PUT /api/cv-presentations/{id}/experience-selections` | `ReplaceExperienceSelectionsCommand` | `ReplaceExperienceSelectionsCommandHandler` | `ReplaceExperienceSelectionsEndpoint` | 4 |
| 17 | `ReplaceEducationSelectionsUseCase` | `PUT /api/cv-presentations/{id}/education-selections` | `ReplaceEducationSelectionsCommand` | `ReplaceEducationSelectionsCommandHandler` | `ReplaceEducationSelectionsEndpoint` | 4 |
| 18 | `ReplaceSkillSelectionsUseCase` | `PUT /api/cv-presentations/{id}/skill-selections` | `ReplaceSkillSelectionsCommand` | `ReplaceSkillSelectionsCommandHandler` | `ReplaceSkillSelectionsEndpoint` | 4 |
| 19 | `ReplaceLanguageSelectionsUseCase` | `PUT /api/cv-presentations/{id}/language-selections` | `ReplaceLanguageSelectionsCommand` | `ReplaceLanguageSelectionsCommandHandler` | `ReplaceLanguageSelectionsEndpoint` | 4 |
| 20 | `ReplaceCertificationSelectionsUseCase` | `PUT /api/cv-presentations/{id}/certification-selections` | `ReplaceCertificationSelectionsCommand` | `ReplaceCertificationSelectionsCommandHandler` | `ReplaceCertificationSelectionsEndpoint` | 4 |
| 21 | `ReplaceProjectSelectionsUseCase` | `PUT /api/cv-presentations/{id}/project-selections` | `ReplaceProjectSelectionsCommand` | `ReplaceProjectSelectionsCommandHandler` | `ReplaceProjectSelectionsEndpoint` | 4 |
| 22 | `ReplaceProfileLinkSelectionsUseCase` | `PUT /api/cv-presentations/{id}/profile-link-selections` | `ReplaceProfileLinkSelectionsCommand` | `ReplaceProfileLinkSelectionsCommandHandler` | `ReplaceProfileLinkSelectionsEndpoint` | 4 |
| 23 | `ExportCVPresentationUseCase` | `GET /api/cv-presentations/{id}/export` | `ExportCVPresentationQuery` | `ExportCVPresentationQueryHandler` | `ExportCVPresentationEndpoint` | 4 (binary contract completed in 8) |
| 24 | `GetCurrentUserUseCase` | `GET /api/me` | `GetCurrentUserQuery` | `GetCurrentUserQueryHandler` | `GetCurrentUserEndpoint` | 4 |
| 25 | `LoginUseCase` | `POST /auth/login` | `LoginCommand` | `LoginCommandHandler` | `LoginEndpoint` | 4 |
| 26 | `CallbackUseCase` | `GET /auth/callback` | `CompleteAuthCallbackCommand` | `CompleteAuthCallbackCommandHandler` | `CompleteAuthCallbackEndpoint` | 4 |
| 27 | `RefreshUseCase` | `POST /auth/refresh` | `RefreshSessionCommand` | `RefreshSessionCommandHandler` | `RefreshSessionEndpoint` | 4 |
| 28 | `LogoutUseCase` | `POST /auth/logout` | `LogoutCommand` | `LogoutCommandHandler` | `LogoutEndpoint` | 4 |

Counts: 10 ProfessionalProfile operations (rows 1 to 10, matching the controller's 10 actions),
13 CVPresentation operations (rows 11 to 23, matching its 13 actions), 1 identity query, 4 auth
commands. Two migrate in Phase 3, twenty-six in Phase 4.

**Routes with no application operation**, which therefore appear in no row above and are not lost:
`GET /api/health` (operational probe, stays outside dispatch), `GET /auth/csrf` (antiforgery token
issuance, a transport concern), `POST /auth/e2e/session` (E2E-only, no production meaning),
`* /api/{**catchall}` and `* /auth/{**catchall}` (404 routing), and the `MapFallbackToFile` SPA shell.
Phase 6 keeps each as a one-action endpoint class; none gains or loses a route.

---

## 9. Verification strategy

Per-phase block (run all; report anything that could not execute as *not executed*, never as
passing):

```bash
cd backend && dotnet restore --locked-mode && dotnet build --no-restore --warnaserror && dotnet test --no-build && dotnet format --verify-no-changes && dotnet list package --vulnerable --include-transitive
```

```bash
cd frontend && npm ci && npm run generate:api && git diff --exit-code -- src/api/generated && npx eslint . && npx tsc --noEmit -p tsconfig.app.json && npm test && npm run build && npm audit --audit-level=high
```

Phase-specific gates:

- **1** — the authorization-inventory test fails on a missing declaration.
- **3** — the pilot integration test hits real PostgreSQL via Testcontainers.
- **4** — `RlsIsolationPhase2Tests` still proves cross-owner denial.
- **6** — no path/verb/success diff in `schema.d.ts`.
- **7** — exact `code`/`type`/`status`/`pointer` asserted; numeric-versus-string variants rejected.
- **8** — OpenAPI-shape test for the binary export plus a fail-closed startup test.
- **9** — localization key-set check.
- **10** — egress-disabled run.

Baseline first: before Phase 1, record branch, revision, clean tree, and the current pass/fail state
of every command above — including whether Docker is available for Testcontainers — so nothing
pre-existing is misattributed to the migration.

Non-gating but required at the end of Phases 6, 7, and 8: `npm run e2e:full` (journeys 001 and 004).
E2E stays post-merge/manual per the project contract.

---

## 10. Decisions that require explicit approval

> **Resolved 2026-08-21.** The owner directed that the canonical contract wins and that local
> decisions in conflict with it are not preserved. That settles **A** (reverse ADR-0008 — ADR-0026),
> **B** (option 1, the standards-aligned path — ADR-0028), **D** (migrate to NSwag in Phase 8; no
> retention ADR is written), **F** (feed provisioned and reachable; pinned at the stable `Devalente.Shared.* 0.2.0`), **G**
> (S2 — ADR-0027), and **H** (skip auditing abstractions). **C** and **E** remain open: both are
> answered by the contract in principle, but the exact observable-contract cutover and the localization
> scope are confirmed when Phases 7 and 9 are implemented. The original text is kept below because it
> records why each decision went the way it did.

**A. Reverse ADR-0008.** The contract mandates CQRS vertical slices with canonical operation names
and `IApplicationMediator`; ADR-0008 forbids exactly that. Adopting the standards means superseding
it (ADR-0013 stays valid and aligned). Confirm the reversal, or declare a permanent recorded
deviation — in which case Phases 3–7 shrink to the DbContext boundary, Results, and Problem Details
only.

**B. Transaction and RLS ownership (the one hard technical decision).** Today
`RlsTransactionActionFilter` opens the transaction, runs `set_config(…, is_local: true)`, and commits
inside the MVC action stage — deliberately, so the commit precedes any response bytes.
`Devalente.Shared.EntityFrameworkCore`'s command behavior also wants to own begin/save/commit, and
explicitly does **not** save or commit when a transaction already exists. Options:

1. **Recommended:** move the RLS owner scope into a mediator pipeline behavior registered *inside*
   the EF transaction behavior (validation → EF transaction → RLS scope → handler), so `set_config`
   still runs within an existing transaction and the shared behavior owns save/commit. The commit
   still happens inside the action, before the result stage. Requires deleting the action filter and
   the `[UsesOwnerScopedData]` attribute.
2. Keep the action filter as the outer transaction owner and mark commands
   `IManualTransactionCommand<TResult>`, with handlers owning their saves. Preserves today's
   semantics exactly; carries the standard's escape hatch on every command.

**C. Observable HTTP contract changes.** Phase 7 changes error bodies for every failure (bare
404/409/422 and the `outcomeCode` extension → `ApiProblemDetails` with `errors[]`), Phase 10's matrix
may change an authorization outcome from 404 to 403 or vice versa, and Phase 8 changes the export
operation's declared response. With one real user and a same-repository frontend the blast radius is
minimal, but this is a public-contract change: approve changing it in place, or require a
compatibility window.

**D. Client generator.** NSwag is the standard default; `openapi-typescript` + `openapi-fetch` is
already working, reproducible, and drift-checked. The standard permits retaining it **with an ADR**
recording why changing it has no current architectural benefit, since there is no C# consumer.
Recommendation: retain under ADR, revisit when a C# consumer or an unsupported contract requirement
appears. Migrating instead means rewriting `api/client.ts` (including the single-flight refresh and
401-retry middleware) and both feature adapters — real work with no current payoff. Note: the
fail-closed startup validation in #18 is blocked either way until build-time document generation
stops running the host, so if `openapi-typescript` is kept, Phase 8 must still solve that separately
(a design-time-only configuration guard).

**E. Localization scope.** Adopt the `errors` namespace only, default locale `en` (recommended — the
app's `locale` today is a CV-export concern, not a UI language), or take on full UI localization and
a second locale now?

**F. Private feed access.** Phase 1 is blocked without GitHub Packages read access for both the
development machine and CI, and `Devalente.Shared.*` is `0.x` with breaking changes allowed in minor
versions. Confirm the feed is available and that pinning to a specific `0.x` version for the whole
migration is acceptable.

**G. Security profile S2.** Recommended and, most likely, uncontroversial (authenticated user,
durable personal data, internet-intended). Once confirmed it is recorded in an ADR without further
discussion.

**H. Optional, default "no".** Adopting `Devalente.Shared.Auditing.Abstractions` (#27) would change
persisted columns and require a migration; there is no current traceability requirement. The
recommendation is to skip it and keep the existing timestamps.

---

## 11. Risks and rollback boundaries

| Risk | Boundary / mitigation |
|---|---|
| **No persisted-data change in the entire plan** (unless H is approved) | Every phase is schema-neutral; rollback is a `git revert` with no data unwind. This is the single biggest safety property of this migration. |
| Phase 4 volume (26 slices, 3 repositories deleted) | One PR per feature folder, each independently green; the Phase 3 pilot must be reviewed *before* the pattern repeats. |
| RLS regression while repositories are removed | `RlsIsolationPhase2Tests` plus owner-scoped integration tests run in every Phase 3/4 PR; the RLS scripts and policies are never touched. |
| Transaction semantics regression (commit after the response starts) | Decision B is settled in the pilot and asserted by a test that a failed command persists nothing and a successful one is visible before the response completes. |
| Contract drift breaking the SPA or E2E | Routes/verbs/success shapes frozen through Phase 6; the `schema.d.ts` diff is the tripwire; E2E journeys run at 6, 7, and 8. |
| Error-code churn once published | Codes are contracts from Phase 7 onward: append only, never renumber or reuse. Fix the catalog names during Phase 3/4 review, while they are still cheap. |
| Analyzer escalation (#20) flooding the build | Raise in its own Phase 1 commit, separate from architecture; fix or record time-bounded exceptions with an owner — no global suppressions. |
| Formatting churn masking behavior | Mechanical file splitting confined to Phase 2 and to files the later phases rewrite anyway; never combined with an architectural diff. Migrations and structured fixtures excluded from any formatter pass. |
| Private-feed / `0.x` breaking change mid-migration | Pin one version for the whole migration; upgrade the packages in a separate PR from any architectural phase. |
| Scope creep into deferred work | Phase 6c internet deployment, hosting choice, `docs/tbd.md` items, removed features (Study, AI, Job Analyses, Interview Notes, EvidenceLinks), and E2E expansion are all out of scope and stay untouched. |
