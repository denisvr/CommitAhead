---
status: accepted
date: 2026-08-21
---

# ADR-0028: The RLS owner scope is a mediator pipeline behaviour inside the command transaction

## Context

Owner isolation is enforced twice: by application authorization, and by PostgreSQL RLS policies that
read `current_setting('app.current_user_id')`. Setting that value is transaction-scoped —
`set_config(..., is_local: true)` is discarded the moment the transaction ends, which is exactly why a
pooled connection cannot leak it into the next request. It therefore requires a transaction to already
exist.

Today `RlsTransactionActionFilter` (a global MVC action filter) owns that transaction. It opens the
transaction, sets the owner, runs the action, and commits — deliberately inside the action stage, so
the commit completes before the result stage writes any response bytes. An earlier middleware-based
design committed after the response had begun, which could hand a client a success for a write that
had not yet persisted. The filter also re-throws an action exception explicitly, because an action
filter's `next()` returns the exception rather than throwing it, and without the re-throw
`IRlsSessionContext` would commit a failed action's partial writes.

ADR-0026 introduces `Devalente.Shared.EntityFrameworkCore`'s command transaction behaviour, which
wants to own the same boundary: begin, invoke the handler, `SaveChangesAsync` once on a successful
`Result`, then commit or roll back. The contract is explicit that when a transaction already exists,
the automatic behaviour does not save, commit, or roll back, because the external boundary owns that
policy. Two owners cannot both be right, and getting this wrong is silent: writes would appear to
succeed without being saved.

Two compliant arrangements exist. Either the shared behaviour owns the transaction and the RLS scope
runs inside it, or the existing filter keeps ownership and every command declares
`IManualTransactionCommand<TResult>` with handlers owning their own saves.

## Decision

The shared EF Core command behaviour owns the transaction. The RLS owner scope becomes a mediator
pipeline behaviour registered so that it executes **inside** that transaction.

- Registration order is validation, then the EF Core command transaction, then the RLS owner scope,
  then the handler. Validation therefore rejects invalid input before a transaction begins, and
  `set_config` always runs against an existing transaction.
- `RlsTransactionActionFilter` and the `[UsesOwnerScopedData]` attribute are removed. Owner scoping
  stops being opt-in per action and becomes a property of dispatching a request that touches
  owner-scoped data, which removes the failure mode where a new action simply forgets the attribute.
- `IRlsSessionContext` and `RlsSessionContext` are retained as the Infrastructure capability that
  performs `set_config` and owns the change-tracker clearing on failure. The behaviour calls it; it no
  longer owns the transaction lifecycle itself.
- The commit still happens within the MVC action, before the result stage writes the response, because
  dispatch happens inside the action. The property the original filter protected is preserved.
- `IManualTransactionCommand<TResult>` is reserved for a use case that genuinely owns multiple
  transactions or savepoints. It is not used to preserve the current arrangement.

## Verification gate

This decision is settled, but it is not proven until the pilot slice in Phase 3 demonstrates all of
the following, and no command slice merges before they pass:

- a successful command persists, and the data is visible through the public API in the same response;
- a command whose handler returns a failed `Result` persists nothing;
- a command whose handler throws persists nothing and clears the change tracker;
- a query and a command both observe the correct `app.current_user_id`, proven by the existing
  cross-owner RLS isolation tests continuing to pass unmodified;
- a request from one owner cannot read or write another owner's row, proven through the HTTP contract
  as well as at the provider level.

## Consequences

- One transaction owner, expressed in dependency-injection registration order rather than split
  between an MVC filter and a pipeline behaviour.
- Handlers stop calling `SaveChangesAsync`, which is what makes the repository ports removable.
- Owner scoping can no longer be omitted by forgetting an attribute, and it also applies to any future
  non-HTTP adapter that dispatches the same request — the current filter protects only the MVC path.
- The RLS behaviour must apply to queries as well as commands, since queries also read owner-scoped
  tables. Queries receive no automatic transaction from the shared behaviour, so the RLS behaviour
  owns a read transaction for them; that is the one place where it still begins a transaction itself.
- Enabling a retrying EF execution strategy would replay the whole handler, including the RLS scope.
  No such strategy is enabled today; enabling one requires re-reviewing this decision.
- A regression here is silent rather than loud, which is why the verification gate above is a merge
  condition rather than a follow-up task.

## Considered Alternatives

**Keep `RlsTransactionActionFilter` as the outer owner and mark every command
`IManualTransactionCommand<TResult>`.** Preserves today's semantics exactly and needs no reordering.
Rejected: it applies the contract's explicit escape hatch to every single command, which inverts its
intent, keeps `SaveChangesAsync` in application handlers, and leaves owner scoping dependent on an
opt-in attribute and on the MVC pipeline.

**Set the owner per connection instead of per transaction.** Rejected: with connection pooling a
session-scoped setting can leak into a later request on the same physical connection, which is the
precise failure the current `is_local` design avoids.

**Drop RLS and rely on application authorization alone.** Rejected: RLS is deliberate defense in depth
under ADR-0015 and S2 (ADR-0027). The contract also warns that query filters and database policies
must not be the only tenant control — it does not invite removing them.
