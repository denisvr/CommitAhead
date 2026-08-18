---
status: accepted
date: 2026-07-28
---

# AI usage reservation and idempotency are durable

**Status: superseded — this feature was removed from the app (see docs/roadmap.md). Kept for historical record.**

## Context

AI commands need protection from double-clicks, retried HTTP requests, concurrent tabs, process restarts, and daily/monthly cost overruns. An in-memory rate limiter alone cannot provide durable deduplication or atomically reserve budget before a provider call.

## Decision

`AIUsageRecord` is persisted before the provider call with a unique idempotency key and status Reserved. The reservation includes maximum input/output tokens and estimated maximum cost.

The insert transaction checks daily/monthly Completed actual cost plus active Reserved cost before accepting the command. Provider success changes the record to Completed with actual usage, actual cost, and the created AnalysisDraft ID. Failure changes it to Failed and releases unused reservation.

A repeated Completed key returns the existing draft without a provider call. A repeated Reserved key reports that the operation is still in progress. Retrying a Failed operation requires a new explicit idempotency key.

Before every new reservation, Reserved rows older than the configured provider timeout plus a fixed safety margin are lazily transitioned to Failed. This prevents a process crash from consuming budget forever without introducing a background worker.

## Technical implementation: no transaction is held open during the provider call

"Persisted before the provider call" means genuinely committed, in its own database transaction,
before the provider is invoked — not merely inserted-but-uncommitted inside one long-lived
transaction that also spans the external call. `AnalysisCommandOrchestrator` drives three
independently-committed, owner-scoped transactions (via `IRlsSessionContext`):

1. **Reserve** — idempotency/pending-draft checks, the daily/monthly budget check, and the
   `AIUsageRecord` insert (status Reserved) — committed here, before the provider is ever called.
2. *(No transaction)* — the `IAIProvider` call itself. No database connection or transaction is
   held open for its duration; a crash or timeout here can never roll back the already-committed
   reservation.
3. **Complete** — a later, separate transaction creates the `AnalysisDraft` and marks the
   `AIUsageRecord` Completed, atomically.

On failure after a successful reservation, a fourth short transaction marks the already-committed,
durable reservation Failed — it never has to "release" or roll back the reservation itself, since
that row was never part of an open transaction to begin with. The three `AnalyzeX` HTTP actions are
deliberately excluded from the ambient `[UsesOwnerScopedData]`/`RlsTransactionActionFilter`
request-long transaction for exactly this reason.

## Consequences

- Duplicate requests cannot create duplicate provider charges.
- Budget enforcement remains correct across process restarts.
- Provider usage metadata is stored, but prompts, responses, and user-authored content never are.
- Process crashes leave a temporary Reserved record, but lazy reconciliation releases it on the next AI command after the deterministic timeout.

## Considered Alternatives

In-memory idempotency and counters were simpler but reset on deployment and cannot atomically coordinate budget checks with record creation. Persisting usage only after provider completion leaves a race in which concurrent calls can all pass the same budget check. Both were rejected.
