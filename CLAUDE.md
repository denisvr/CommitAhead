@../engineering-standards/ENGINEERING.md
@docs/engineering-context.md

# CommitAhead — Claude Code adapter

The imported engineering contract is the canonical instruction source. Use its task routing to read
only the detailed standards relevant to the current work. `docs/engineering-context.md` records this
project's own context and binding project decisions.

This file is a discovery adapter. It must not restate architecture, security, style, or testing
rules — those live in the contract.

## Before starting work

1. Read `docs/current-state.md` for the current status, priority, and explicit deferrals. Do not
   infer that a later roadmap phase has started.
2. Read `CONTEXT.md` for terminology, then the relevant documents under `docs/`.
3. Read every ADR that affects the requested change.
4. Check `docs/tbd.md`. Never resolve an open decision by assumption.
5. Before frontend work, read the design-system documents named in
   `docs/engineering-context.md`.

The repository is mid-migration onto the contract; see
`docs/migration/engineering-standards-adoption-plan.md`. Follow the target architecture, not the
surrounding legacy shape.

## Commands

Backend, from `backend/`:

```bash
dotnet restore --locked-mode && dotnet build --no-restore --warnaserror && dotnet test --no-build && dotnet format --verify-no-changes
```

Frontend, from `frontend/`:

```bash
npm ci && npm run generate:api && npx eslint . && npx tsc --noEmit -p tsconfig.app.json && npm test && npm run build
```

`README.md` owns local development, the local production-like runtime, and manual acceptance
commands. `e2e/README.md` owns the E2E commands — run them only when explicitly requested.

## Changing an accepted decision

If an implementation requires changing an accepted architectural decision, supersede the relevant
ADR explicitly before changing code, and keep the roadmap, tests, and security documentation
consistent with the new decision.
