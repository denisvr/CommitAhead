# CommitAhead — agent instructions

Before doing any work, read and follow:

1. `../engineering-standards/ENGINEERING.md` — the canonical engineering contract
2. `docs/engineering-context.md` — this project's context and binding project decisions

Use the contract's task routing to read only the standards relevant to the current work. Project
instructions may strengthen the contract but cannot weaken its non-negotiable rules.

This file is a discovery adapter. It must not duplicate or override engineering rules from the
contract.

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

## Changing an accepted decision

If an implementation requires changing an accepted architectural decision, supersede the relevant
ADR explicitly before changing code, and keep the roadmap, tests, and security documentation
consistent with the new decision.
