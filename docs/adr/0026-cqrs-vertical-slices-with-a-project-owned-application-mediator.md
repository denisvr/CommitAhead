---
status: accepted
date: 2026-08-21
---

# ADR-0026: CQRS vertical slices with a project-owned application mediator

**Supersedes ADR-0008** ("Feature-folder use cases without MediatR").

## Context

ADR-0008 excluded MediatR and, with it, any dispatch abstraction: every operation became a concrete
`*UseCase` class with a single `ExecuteAsync`, injected directly into a thin controller. The
reasoning was that runtime handler resolution was not needed and that direct call sites are easier to
navigate. That reasoning was sound on its own terms, and it produced 28 working use cases.

The canonical contract adopted in ADR-0025 requires something different, and for reasons ADR-0008 did
not weigh:

- CQRS with explicit command/query separation and canonical operation naming
  (`<Operation>Command`, `<Operation>CommandHandler`, `<Operation>CommandResult`,
  `<Operation>Validator`), organized as vertical slices under
  `Features/<Feature>/{Commands,Queries}/<Operation>/`.
- Dispatch through the project-owned `IApplicationMediator` from `Devalente.Shared.Cqrs` — explicitly
  **not** MediatR.
- A central, ordered pipeline where validation, the EF Core command transaction, and other
  cross-cutting concerns are composed once instead of per operation.

ADR-0008's substitute for that pipeline was "ASP.NET middleware, filters, and decorators". In practice
that produced a global exception filter mapping `DomainValidationException` to 422, a global action
filter owning the RLS transaction, and no validation layer at all — cross-cutting concerns spread
across three different ASP.NET extension points, each with its own lifecycle relative to the response.

The two objections in ADR-0008 do not apply to the contract's mediator:

- It is project-owned, not MediatR, so the third-party-abstraction objection is moot.
- Handler discovery must report missing and duplicate registrations loudly rather than silently
  choosing one, so the "harder to trace" objection is bounded: one request type has exactly one
  handler, and that is verified.

## Decision

Backend use cases are organized as CQRS vertical slices and dispatched through
`IApplicationMediator`.

- A command changes state; a query reads state. Each has exactly one handler implementing one use
  case.
- Each operation owns its input, handler, result, and validator when it needs one, in its own folder,
  one top-level type per file, using the canonical names above.
- MVC endpoints inject `IApplicationMediator`, send exactly one command or query, and translate the
  result. Liveness/readiness probes and the `/api` and `/auth` 404 catch-alls remain outside dispatch,
  as the contract's operational exception allows.
- Validation runs as a pipeline behaviour before the handler and returns failures through the
  request's `Result`. Expected validation stops being exception control flow, and
  `DomainValidationException` stops being a transport concern.
- MediatR remains prohibited. Generic `IUseCase<TRequest, TResponse>` interfaces remain prohibited.
  The per-operation `*UseCase` class with `ExecuteAsync` is retired.

Migration is phased per `docs/migration/engineering-standards-adoption-plan.md`: a reviewed pilot
slice first, then one pull request per feature folder. Routes, verbs, and success status codes stay
byte-identical while the internals move, so the SPA, the generated client, and both E2E journeys are
unaffected by the restructure itself.

## Consequences

- Cross-cutting behaviour is composed once in a visible, ordered pipeline instead of being spread
  across a global exception filter, a global action filter, and per-use-case code.
- Operation contracts become uniform and predictable, which is what makes the shared Result and
  RFC 9457 Problem Details mapping possible without per-endpoint special cases.
- Navigation changes: `F12` on a call site reaches the mediator, not the handler. The compensating
  controls are canonical naming, one handler per request type, and loud discovery failures.
- File count grows substantially — roughly 25 operations become slices with three to four small files
  each. That is the contract's explicit trade-off in favour of locating a use case quickly.
- A dependency on `Devalente.Shared.Cqrs` and `Devalente.Shared.Cqrs.Abstractions` is introduced, so
  the code phases are blocked until the private feed is reachable.
- Transaction ownership must be settled before the first command slice lands, because the shared EF
  Core command behaviour and the existing RLS transaction filter both want to own it. That is
  ADR-0028.

## Considered Alternatives

**Keep ADR-0008 and record a permanent deviation.** Rejected by ADR-0025: mediator dispatch and
canonical CQRS structure are core non-negotiable rules, not peripheral preferences, and retaining the
deviation would also forfeit the validation and transaction behaviours the rest of the migration
depends on.

**Adopt the naming and folder structure but keep direct handler injection, without a mediator.**
Rejected: the contract acknowledges CQRS does not inherently require a mediator, but uses
`IApplicationMediator` as the consistent dispatch boundary precisely so the pipeline exists. Without
it, validation and command transactions return to per-operation code.

**Adopt MediatR.** Prohibited by the contract, and it was already rejected by ADR-0008 for reasons
that still hold.
