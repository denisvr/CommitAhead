# CommitAhead — Persistence Strategy

## Approach

EF Core 10 code-first migrations against PostgreSQL (Supabase). The `CommitAheadDbContext` is the single database entry point. Migrations are the authoritative schema source.

## Key Mapping Decisions

### Typed category details (discriminated union)
`StudyItem.details` is a discriminated union of `LeetCodeDetails`, `SystemDesignDetails`, `BehavioralDetails`, and `TheoryDetails`. The persistence strategy for this union is **TBD** (see `docs/tbd.md`). Candidates:
- **JSONB column** on `StudyItems` table — simple, schema-less, but queries on detail fields are less ergonomic.
- **Table-per-concrete-type** — `LeetCodeDetails`, `SystemDesignDetails`, etc. as separate tables with a 1:1 FK to `StudyItems`. More relational, but adds joins.

The chosen approach will be documented here once decided.

### Polymorphic source reference (EvidenceLink, AnalysisDraft)
Both `EvidenceLink` and `AnalysisDraft` carry `sourceType` (enum column) + `sourceId` (UUID column). No database foreign key can enforce this across three different target tables. Referential integrity is enforced by the application layer (use case validates existence before creating a link or draft).

### JobSource (discriminated union)
Stored as a `sourceKind` discriminator column + `pastedContent` (nullable text) + `storageObjectKey` (nullable text) + `originalFileName` (nullable text) + `mimeType` (nullable text) + `extractedText` (nullable text) on the `JobAnalyses` table.

### EvidenceLink uniqueness
A unique database constraint on `(source_type, source_id, target_study_item_id)` enforces the "at most one link per source–StudyItem pair" invariant.

### One-Pending-draft-per-source
A partial unique index on `(source_type, source_id) WHERE status = 'Pending'` enforces the "at most one Pending draft per source" invariant.

### StudyItem deletion guard
A non-cascade FK from `evidence_links.target_study_item_id` to `study_items.id` ensures that a StudyItem cannot be hard-deleted while any EvidenceLink references it — even if the application-level guard is bypassed.

### Tags
Stored as a `TEXT[]` PostgreSQL array column on `StudyItems`. Normalisation (trim, lowercase, kebab-case, deduplication) is applied in the domain before persisting. EF Core maps `string[]` to `TEXT[]` via Npgsql.

### YearMonth
Stored as two integer columns (`_year`, `_month`) in the parent table. No native `YearMonth` DB type.

### Proposal collections (AnalysisDraft)
Three separate tables: `suggestion_proposals`, `link_proposals`, `study_item_proposals`, each with an FK to `analysis_drafts.id`. The `SuggestionProposal` discriminated union (StructuredSuggestion vs AdvisorySuggestion) uses a `kind` column + a JSONB `payload` column for the structured command payload.

## Migration Strategy

- Migrations are applied once per deployment, before the API starts, using `dotnet ef database update` or an equivalent startup command.
- Each migration is reviewed before merging — no auto-generated migrations are applied unreviewed.
- Breaking schema changes (column renames, type changes) are split into additive migrations with a deprecation period.
- Integration tests run against a Testcontainers PostgreSQL instance with migrations applied once per test session; Respawn resets data between tests (not schema).

## ScoringConfig
A `scoring_config` table with at most one row (no user ID scope — single-user app). The row is absent when defaults apply; it is created on first user save. The application layer never assumes the row exists.

## Supabase RLS
Row-Level Security is enabled on all application tables with **no** `anon` or `authenticated` policies. Direct client access is denied at the database level. The backend uses a least-privileged service credential for all table access; the Supabase service-role key is reserved for Auth and Storage administration only.
