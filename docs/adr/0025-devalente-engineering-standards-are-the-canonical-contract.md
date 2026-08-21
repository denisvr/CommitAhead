---
status: accepted
date: 2026-08-21
---

# ADR-0025: The Devalente engineering standards are the canonical engineering contract

## Context

CommitAhead's engineering rules were written inside the project itself — `CLAUDE.md` held the layer
table, the hard constraints, and the CI gate inventory, and `AGENTS.md` repeated a subset of them.
Those rules were sound, but they were project-local: nothing connected them to a reusable contract,
and several of them had drifted into decisions that a shared standard should own.

A separate standards repository now exists (`../engineering-standards`) with a tool-neutral entry
point, `ENGINEERING.md`, that defines non-negotiable implementation rules, task routing to detailed
standards, and a family of `Devalente.Shared.*` packages implementing the stable parts. Its adoption
guide prescribes a project-owned `docs/engineering-context.md` plus thin tool adapters, explicitly so
that the standards are never copied into a consumer where they would drift from their source.

Some accepted CommitAhead decisions conflict with that contract. The most material is ADR-0008, which
excluded mediator dispatch and canonical CQRS operation naming.

## Decision

The Devalente engineering standards are the canonical engineering contract for CommitAhead.

- `ENGINEERING.md` in the sibling standards checkout is the authoritative source for architecture,
  CQRS structure, results and errors, security baseline, C# style, and testing levels.
- `CLAUDE.md` and `AGENTS.md` become discovery adapters that import the contract and
  `docs/engineering-context.md`. They keep reading order and project commands, and no engineering
  rules.
- `docs/engineering-context.md` records only what an agent cannot safely rediscover: root namespace,
  topology, security profile, stack choices, and the project decisions that genuinely strengthen the
  contract.
- Project documentation may strengthen or specialize the contract. It may not weaken it. Where a
  project document disagrees with the contract, the contract applies and the project document is
  corrected — file load order is never the tie-breaker.
- A local decision that contradicts a non-negotiable rule is superseded by a new ADR, not retained
  as an accidental instruction conflict. ADR-0008 is superseded by ADR-0026 on that basis.
- The consumed standards revision and shared-package version are pinned in
  `docs/engineering-context.md` and updated deliberately, never as a side effect of another change.

Adoption proceeds in verified phases per
`docs/migration/engineering-standards-adoption-plan.md`, not as a single rewrite.

## Consequences

- One contract governs this project and any future sibling project, and it is maintained in one
  place instead of being copied per repository.
- Decisions that were project-local preferences (mediator dispatch, persistence abstraction shape,
  expected-failure modelling, HTTP error contract) are now settled by the contract, and the project
  stops re-litigating them.
- The project takes on a dependency on a sibling checkout being readable, and on the private
  `Devalente.Shared.*` feed being reachable from developer machines and CI. A missing feed blocks
  the code phases of the migration.
- The codebase is temporarily inconsistent: legacy use-case classes and repository ports coexist with
  the target shape until the migration completes. New work follows the target, and
  `docs/engineering-context.md` says so explicitly so the surrounding legacy shape is not copied.
- Documentation that described the superseded architecture (`CLAUDE.md`, `docs/architecture/solution.md`,
  `docs/testing/strategy.md`) must be reconciled as the corresponding code phases land.

## Considered Alternatives

**Keep the rules project-local and copy improvements in by hand.** Rejected: copied standards drift
from their source, and the adoption guide prohibits pasting the documentation into a consumer.

**Adopt the contract but record every conflicting local decision as an approved deviation.** Rejected:
it would preserve exactly the accidental conflicts the contract is meant to remove, and the
deviations in question (mediator dispatch, direct EF Core persistence) are core to the standard
rather than peripheral.

**Vendor the standards as a Git submodule.** A documented fallback in the adoption guide for sandboxes
that cannot read sibling directories. Not needed here — the sibling checkout is readable — and a
submodule adds pinning ceremony this project does not currently need.
