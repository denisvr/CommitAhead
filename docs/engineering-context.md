# Engineering context

This file is owned by CommitAhead, not by the standards repository. It records only what a coding
agent cannot safely rediscover on every task. Everything else — layers, dependency direction, CQRS
structure, result and error contracts, security baseline, testing levels — lives in the canonical
contract and must not be restated here.

- Standards revision: `b199c5f4561d6ab725868d2e6f036ba04ec093e3` (2026-08-20)
- Standards checkout: `../engineering-standards` (sibling directory; read-only for this project)
- Shared package version: `Devalente.Shared.* 0.2.0` (stable, tagged `v0.2.0`). `0.x` allows breaking
  changes in a minor version, so keep this pin for the whole migration and bump it in its own commit,
  separately from any architectural phase
- Root namespace: `CommitAhead` (no organization prefix — preserve it; renaming requires explicit
  authorization)
- Topology: modular monolith — one deployable ASP.NET Core host that also serves the React SPA from
  `wwwroot`
- Security profile: **S2** (ADR-0027)
- Backend: ASP.NET Core MVC on `net10.0`, SDK pinned by `backend/global.json`
- Frontend: React 19 + Vite, TypeScript, CSS Modules (ADR-0016)
- Persistence: Entity Framework Core (Npgsql / PostgreSQL)
- Generated clients: TypeScript for the SPA. Currently `openapi-typescript`; **NSwag is the
  target** — this is a migration state, not an approved deviation (see "In-flight migration")

## Precedence

The canonical contract wins. Project documentation may strengthen or specialize it, never weaken it.
When a project document and the contract disagree, the contract applies and the project document is
corrected — do not rely on file load order to pick a winner.

Behaviour, terminology, and domain rules remain owned by `CONTEXT.md`, `docs/domain/`, and
`docs/adr/`. Those are specializations, not conflicts.

## Project decisions

These strengthen the shared contract and are binding.

- **Private and invite-only.** Public signup stays disabled. Data is isolated per user by
  `OwnerUserId` from the start (ADR-0015), and PostgreSQL RLS enforces it as defense in depth on top
  of application authorization — never instead of it. Today there is exactly one real user; that is
  not a licence to skip owner scoping anywhere.
- **All Supabase keys are backend-only.** The browser never talks to Supabase directly. Development
  authenticates against a fully local Supabase instance (ADR-0023, `supabase start`); Supabase Cloud
  is reserved for production, which has not started.
- **The frontend never recomputes values the backend owns.** CV export eligibility (template, photo,
  page limit), locale date formatting rules, and validation outcomes are rendered from API responses.
- **Studio with the Bookmark mark is the only approved visual direction** (ADR-0024). Before any
  frontend work read `docs/design/design-system/readme.md`, `components.md`, and
  `page-patterns.md`. Design reference HTML under `docs/design/` is a reference, never copied into
  `frontend/`. `frontend/src/design-system/tokens/` mirrors the reference tokens: change a value in
  both, in the same commit. Every screen must work in light, in dark, and with no explicit theme
  choice under a dark system preference.
- **The CSP has no `unsafe-inline` for `style-src`.** This is why drag reordering uses native HTML5
  drag and not a JS drag library — see `CollapsibleRow.tsx`. Do not introduce a dependency that
  needs inline styles.
- **E2E is never part of ordinary feature development or PR validation.** Run it only when explicitly
  requested, or when directly changing something under `e2e/`. Exactly two approved journey files
  exist under `e2e/tests/journeys/` — no more, no fewer. Zero real Supabase calls, ever. Read
  `docs/testing/strategy.md` Layer 7 (normative) and `e2e/README.md` (runbook) first.
- **Removed features stay removed.** Study, Job Analyses, Interview Notes, the AI analysis pipeline,
  and EvidenceLinks were deliberately deleted on 2026-08-18, not deferred. Re-introducing any of them
  requires an explicit product decision.
- **Never resolve an open decision by assumption.** `docs/tbd.md` owns unresolved decisions.
  Phase 6c internet deployment and hosting selection require explicit authorization before any work.

## Private package feed

`Devalente.Shared.*` comes from a private GitHub Packages feed. `NuGet.Config` at the repository root
declares the source with **no credential** and pins the `Devalente.Shared.*` prefix to it through
`packageSourceMapping`, so a same-named package on nuget.org can never be substituted.

Credentials live in two places, neither of them this repository:

- **Locally** — in the user-level NuGet configuration only. Register it with
  `dotnet nuget add source`, using the source name `devalente-shared` exactly, because NuGet matches
  stored credentials by source name rather than URL.
- **In CI** — a repository secret named `DEVALENTE_PACKAGES_TOKEN`, injected into the runner's
  ephemeral checkout before restore. The workflow fails closed with a named error when it is absent,
  rather than letting restore fall back to a 401 that surfaces as a confusing "package not found".

The following must be configured by the repository owner and cannot be represented by files here.
They are tracked as open in the evidence register in `docs/security/threat-model.md`:

1. `DEVALENTE_PACKAGES_TOKEN` under Settings, Secrets and variables, **Actions** — a classic PAT with
   `read:packages` and nothing else.
2. The same value under Settings, Secrets and variables, **Dependabot**. This is a separate store;
   the Actions secret is invisible to Dependabot, and without it NuGet updates keep running while
   silently losing the ability to evaluate the private packages.
3. The package read grant. NuGet packages on GitHub Packages inherit permissions from the repository
   that published them, so a cross-repository `GITHUB_TOKEN` does not generally work. If the package
   settings page offers "Manage Actions access", granting `CommitAhead` read access there allows the
   workflows to drop the PAT and use `GITHUB_TOKEN` with the `packages: read` permission they already
   declare.

If a freshly published version is invisible to restore, it is almost always the local NuGet HTTP
cache, which holds the feed index for about 30 minutes: `dotnet nuget locals http-cache --clear`.

## Reading order

`docs/current-state.md` (status and current priority) → `CONTEXT.md` (terminology) → the relevant
documents under `docs/` → every ADR affecting the change → `docs/tbd.md`.

Document owners: `docs/roadmap.md` owns implementation status; `docs/current-state.md` owns the
operational handoff; `CONTEXT.md` owns terminology; `docs/testing/strategy.md` Layer 7 owns E2E
rules; `README.md` owns local development and runtime commands; `docs/adr/` owns accepted decisions;
`.github/workflows/ci.yml` owns the concrete quality gates.

## In-flight migration

`docs/migration/engineering-standards-adoption-plan.md` is the adoption plan and gap analysis. The
repository is mid-migration: current code still uses per-operation use-case classes, repository
ports, and non-RFC-9457 error responses. New work follows the target architecture in that plan and
in the contract, not the surrounding legacy shape.

Phases 0 and 1 are done; `docs/current-state.md` is the authority on what is next and what is still
open. Packages are installed per phase, only where the phase's code needs them — do not add a
`Devalente.Shared.*` reference ahead of the code that uses it.

## Approved deviations

None. The generator state above is a tracked migration target, not a deviation. ADR-0008 was
superseded by ADR-0026 rather than kept as an exception.
