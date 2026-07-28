---
status: accepted
date: 2026-07-28
---

# PostgreSQL on Supabase rather than SQLite

## Context

A private single-user app is the canonical use case for SQLite: zero infrastructure, no network round-trip, trivially portable. The decision was which database engine and host to use for MVP.

## Decision

PostgreSQL hosted on Supabase is used for the database. Supabase also provides Auth and Storage, consolidating three infrastructure concerns into one platform.

**Supabase hosts**: PostgreSQL, Auth, and Storage only. The hosting platform for the ASP.NET Core API and the Vite/React frontend is a separate decision and remains **TBD** (see `docs/tbd.md`).

## Consequences

- Schema benefits from a full relational engine: polymorphic EvidenceLink source references, FK constraint enforcement for deletion guards, ranked-list queries with aggregations across joined tables, and partial unique indexes for the one-Pending-draft-per-source invariant are all more natural in PostgreSQL.
- SQLite's limited `ALTER TABLE` support would create friction as the schema evolves across migrations.
- A network round-trip to Supabase is introduced for every query; this is acceptable for a single-user app with low concurrency.
- Minimal APIs were evaluated alongside Controllers and excluded in favour of Controllers, which offer clearer structure for the feature-folder architecture (see ADR-0008).

## Considered Alternatives

SQLite with a self-hosted or embedded deployment was the main alternative. It was rejected primarily because it would not integrate with Supabase Auth and Storage, requiring separate services for those concerns, and because the schema complexity (see Consequences above) favours PostgreSQL.
