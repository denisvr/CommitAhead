---
status: accepted
date: 2026-08-21
---

# ADR-0028: The RLS owner scope is a mediator pipeline behaviour inside the command transaction

> Revised 2026-08-21, before any implementation, to specify the mechanics precisely and to withdraw
> an incorrect claim about what the existing RLS tests prove. The decision itself is unchanged.

## Context

Owner isolation is enforced twice: by application authorization, and by PostgreSQL RLS policies that
read `app_current_owner_user_id()`, a helper over `current_setting('app.current_user_id', true)`.
Setting that value is transaction-scoped — `set_config(..., is_local: true)` is discarded the moment
the transaction ends, which is exactly why a pooled connection cannot leak it into the next request.
It therefore requires a transaction to already exist.

Today `RlsTransactionActionFilter`, a global MVC action filter, owns that transaction. It opens the
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

## Decision

The shared EF Core command behaviour owns command transactions. The RLS owner scope becomes a
mediator pipeline behaviour that runs **inside** that transaction.

### Registration order

Behaviours execute in registration order, outermost first:

```text
validation  ->  EF Core command transaction  ->  RLS owner scope  ->  handler
```

Validation is outermost so an invalid request is refused before a transaction begins. The owner scope
is innermost so `set_config` always runs against a transaction that already exists for commands.

### Identifying an owner-scoped request

The scope applies whenever a request executes with an authenticated application user, that is
whenever `ICurrentUser.UserId` is not `Guid.Empty`. There is no marker interface, no attribute, and
no per-request opt-in.

This is the whole point of the change. `[UsesOwnerScopedData]` had to be remembered on every new
action; the condition above cannot be forgotten, because there is nothing to add. Anonymous flows
(login, callback, refresh, logout, CSRF issuance, health) have no authenticated user and correctly
receive no scope.

A future operation that must run authenticated *without* the owner scope — an administrative or
purge use case — requires an explicit, reviewed opt-out and its own ADR. The direction of failure for
a missing opt-out is that the operation sees nothing, which is fail-closed; the direction of failure
for a forgotten marker would have been that it sees everything.

Because there is no marker, the mechanical guard is not an inventory of marked requests but a test
over the pipeline itself:

- a test that inspects the registered behaviour sequence and fails if the order above changes, in
  particular if the owner scope is ever registered outside the transaction behaviour;
- a test proving the behaviour applies to a query as well as a command, so neither path can silently
  lose the scope.

### Command path

The transaction already exists when the behaviour runs. It must therefore:

1. observe that `dbContext.Database.CurrentTransaction` is not null;
2. **not** open a transaction, a nested transaction, or a savepoint;
3. execute `set_config('app.current_user_id', <owner>, true)` on that existing transaction;
4. invoke the rest of the pipeline;
5. return without saving, committing, or rolling back — all three stay with the outer EF behaviour.

### Query path

Queries receive no automatic transaction, so for a query the behaviour opens one, sets the owner,
invokes the handler, and commits or rolls back that read transaction itself. This is the only case in
which the behaviour owns a transaction.

### Retained and removed

`IRlsSessionContext` and `RlsSessionContext` are retained as the Infrastructure capability that
performs `set_config` and clears the change tracker on failure; they no longer own the transaction
lifecycle for commands. `RlsTransactionActionFilter` and `[UsesOwnerScopedData]` are deleted in
Phase 5, once no operation depends on them.

`IManualTransactionCommand<TResult>` is not used. A command that adopted it would own its own
transaction lifecycle and would invalidate the command path above, so introducing one requires
re-reviewing this ADR.

## Verification gate

The decision is settled; it is not proven until the tests below pass. **No command slice merges
before they do.**

### What the existing tests do and do not prove

`RlsIsolationPhase2Tests` constructs `RlsSessionContext` and calls it directly. It proves that the
policies, the `commitahead_app` grants, and `set_config` behave correctly at the provider level, and
it is kept for exactly that. It exercises no mediator behaviour, no pipeline order, and no HTTP path,
and it must not be cited as evidence that the pipeline is wired correctly.

`PostgresApiTestFactory` connects as the Testcontainers owner and applies migrations only. It never
applies `001_roles.sql`, `002_rls_users.sql`, or `004_rls_phase2.sql`, and the role it uses is not
subject to RLS at all. No test built on it can prove RLS behaviour, whatever it asserts.

### Required new tests

Real-provider tests exercising dispatch through `IApplicationMediator`:

1. an owner-scoped **query** returns the caller's own data;
2. an owner-scoped **command** persists, and the data is visible through the public API within the
   same response;
3. a command whose handler returns a **failed `Result`** persists nothing;
4. a command whose handler **throws** persists nothing and leaves the change tracker clear;
5. `app.current_user_id` is the caller's id for both a query and a command;
6. a cross-owner **read** is denied;
7. a cross-owner **write** is denied.

### Required test host

The public HTTP tests must run the application against the real least-privileged runtime role, in
this order:

1. apply `001_roles.sql`, creating `commitahead_migrator` and `commitahead_app` with container-only
   generated passwords;
2. apply EF migrations with a privileged role;
3. apply `002_rls_users.sql` and `004_rls_phase2.sql`, which grant table access to `commitahead_app`
   and enable the policies;
4. configure the application's connection string to authenticate as **`commitahead_app`**.

Two consequences follow that an implementer will otherwise hit blind. `commitahead_app` holds only
`SELECT` on `users`, so test users must be seeded through a privileged connection, not through the
application's own connection as `PostgresApiTestHelpers` does today. And `app_current_owner_user_id()`
returns null when the setting is absent, so a missing scope denies access rather than widening it —
which means a test that forgets to authenticate fails closed and cannot produce a false pass.

## Consequences

- One transaction owner, expressed in registration order rather than split between an MVC filter and
  a pipeline behaviour.
- Handlers stop calling `SaveChangesAsync`, which is what makes the repository ports removable.
- Owner scoping cannot be omitted by forgetting an attribute, and it applies to any future non-HTTP
  adapter that dispatches the same request; the current filter protects only the MVC path.
- The commit still happens within the MVC action, before the result stage writes the response,
  because dispatch happens inside the action. The property the original filter protected is preserved.
- A read transaction is opened per query. That is what the current filter already does for
  `[UsesOwnerScopedData]` reads, so it is not a new cost.
- Enabling a retrying EF execution strategy would replay the whole handler, including the owner
  scope. None is enabled today; enabling one requires re-reviewing this decision.
- A regression here is silent rather than loud, which is why the verification gate is a merge
  condition and not a follow-up task.

## Considered Alternatives

**Keep `RlsTransactionActionFilter` as the outer owner and mark every command
`IManualTransactionCommand<TResult>`.** Preserves today's semantics exactly and needs no reordering.
Rejected: it applies the contract's explicit escape hatch to every single command, which inverts its
intent, keeps `SaveChangesAsync` in application handlers, and leaves owner scoping dependent on an
opt-in attribute and on the MVC pipeline.

**Identify owner-scoped requests with a marker interface.** Rejected: a marker is one more thing to
forget on a new request, and the failure mode of forgetting it is that the request sees every owner's
rows. Deriving the scope from the authenticated user removes the omission instead of testing for it.

**Set the owner per connection instead of per transaction.** Rejected: with connection pooling a
session-scoped setting can leak into a later request on the same physical connection, which is the
precise failure the current `is_local` design avoids.

**Drop RLS and rely on application authorization alone.** Rejected: RLS is deliberate defense in
depth under ADR-0015 and the S2 profile (ADR-0027). The contract also warns that query filters and
database policies must not be the only tenant control — it does not invite removing them.
