# PostgreSQL on Supabase rather than SQLite

A private single-user app is the canonical use case for SQLite: zero infrastructure, no network round-trip, trivially portable. We chose PostgreSQL hosted on Supabase because it brings Auth, Storage, and hosting into a single ecosystem without managing separate services. The schema also benefits from a full relational engine: the polymorphic EvidenceLink source reference, FK constraint enforcement for deletion guards, the ranked-list query with aggregations across joined tables, and partial unique indexes for the one-Pending-draft-per-source invariant are all more natural in PostgreSQL. SQLite's limited ALTER TABLE support would create friction as the schema evolves.

Minimal APIs were also considered and excluded in favour of Controllers; they offer less structure for a feature-folder architecture with per-operation use case classes.
