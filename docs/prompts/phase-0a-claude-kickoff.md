# Claude kickoff prompt — Phase 0A

Copy the prompt below into Claude Code from the CommitAhead repository root.

---

You are starting implementation of CommitAhead. This repository is documentation-first and its accepted decisions are authoritative.

Before changing anything, read completely:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `README.md`
4. `CONTEXT.md`
5. `docs/product/brief.md`
6. `docs/product/out-of-scope.md`
7. `docs/domain/model.md`
8. `docs/architecture/solution.md`
9. `docs/architecture/persistence.md`
10. `docs/testing/strategy.md`
11. `docs/security/threat-model.md`
12. `docs/roadmap.md`
13. `docs/tbd.md`
14. Every ADR in `docs/adr/`

Do not reinterpret or silently replace accepted decisions. Do not resolve any item in `docs/tbd.md` by assumption. If this increment unexpectedly depends on a TBD, stop and explain the exact decision required.

## Objective

Implement **Phase 0A only: solution skeleton and architecture baseline**. This must be a small, reviewable first PR. Do not implement PostgreSQL, Supabase, authentication, StudyItems, business aggregates, AI integration, PDF processing, or any later roadmap phase.

## Required scope

### Backend

- Create a .NET 10 solution.
- Create:
  - `src/CommitAhead.Domain`
  - `src/CommitAhead.Application`
  - `src/CommitAhead.Infrastructure`
  - `src/CommitAhead.Api`
- Configure project references according to ADR-0013:
  - Domain has no project dependencies.
  - Application references Domain.
  - Infrastructure references Application and Domain.
  - API references Application and Infrastructure only because API is the composition root.
- Use ASP.NET Core Controllers. Minimal APIs are forbidden.
- Do not add MediatR or generic `IUseCase<TRequest,TResponse>` abstractions.
- Add a single explicitly anonymous health/status Controller endpoint containing no business logic.
- Add Infrastructure and Application DI registration extensions so `Program.cs` remains a composition root rather than a service-registration dump.
- Enable nullable reference types and warnings as errors.

### Tests

- Create:
  - `tests/CommitAhead.Domain.Tests`
  - `tests/CommitAhead.Application.Tests`
  - `tests/CommitAhead.Infrastructure.Tests`
  - `tests/CommitAhead.Api.Tests`
- Use xUnit and built-in assertions; do not add FluentAssertions.
- Add NetArchTest and implement the five architecture rules from `CLAUDE.md`.
- Add one API smoke test for the health/status Controller using `WebApplicationFactory`.
- Do not introduce Testcontainers, Respawn, database fakes, or FakeAIProvider yet; this increment has no persistence or AI.

### Frontend

- Create `src/CommitAhead.Web` using React 19, TypeScript, and Vite.
- Create a minimal CommitAhead application shell with no product feature implementation.
- Add frontend test configuration with Vitest and React Testing Library plus one render smoke test.
- Configure a production build integration so the Vite output can be served by Kestrel from the same origin. Development may use Vite's dev server with an explicit local proxy.
- Do not add a component library yet; that is a Phase 1 TBD.
- Do not add Supabase, AI, external fonts, CDN resources, or unnecessary packages.

### Repository and CI baseline

- Add or update `.gitignore`, deterministic SDK/tool version configuration where appropriate, and basic build scripts.
- Add a GitHub Actions PR workflow that:
  - restores with locked dependencies where supported;
  - runs `dotnet build --warnaserror`;
  - runs backend tests;
  - runs `dotnet format --verify-no-changes`;
  - uses `npm ci`;
  - runs ESLint, `tsc --noEmit`, Vitest, and the production Vite build.
- Pin GitHub Actions to full commit SHAs. If exact trusted SHAs cannot be verified without network access, do not invent them: create the workflow with an explicit blocking TODO and report it.
- Do not add AI, Supabase, Docker, vulnerability scanning, Gitleaks, OpenAPI client generation, E2E, or deployment workflows in Phase 0A; those belong to later Phase 0 increments.

## Implementation rules

- Prefer the smallest dependency set.
- Do not create speculative abstractions, generic repositories, event buses, workers, or background services.
- Preserve all current documentation unless implementation reveals a real contradiction. If it does, report it before changing an accepted ADR.
- Do not commit secrets, generated build outputs, package caches, or local configuration.
- Do not make real external service calls in tests.
- Do not commit or push. Leave the working tree ready for review by Codex.

## Verification

Run every relevant backend and frontend restore, build, format/lint, type-check, and test command. Fix failures that are within this increment.

When finished, stop. Do not continue to Phase 0B.

Return:

1. concise implementation summary;
2. files/projects created;
3. exact commands run and results;
4. any warnings, unresolved issues, or documentation mismatches;
5. proposed Phase 0B scope, without implementing it.

---
