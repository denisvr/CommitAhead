# CommitAhead — Persistence Strategy

## Approach

EF Core 10 code-first migrations against PostgreSQL (Supabase). The `CommitAheadDbContext` is the single database entry point. Migrations are the authoritative schema source.

## Key Mapping Decisions

### Typed category details (discriminated union)
`StudyItem.details` is a discriminated union of `LeetCodeDetails`, `SystemDesignDetails`, `BehavioralDetails`, and `TheoryDetails`. The persistence strategy for this union is **TBD** (see `docs/tbd.md`). Candidates:
- **JSONB column** on `StudyItems` table — simple, schema-less, but queries on detail fields are less ergonomic.
- **Four explicit 1:1 detail tables** — `LeetCodeDetails`, `SystemDesignDetails`, etc. keyed by a FK to `StudyItems`. More relational, but adds joins. This is composition mapping, not EF inheritance/TPT/TPC.

The chosen approach will be documented here once decided.

### Polymorphic source reference (EvidenceLink, AnalysisDraft)
Both `EvidenceLink` and `AnalysisDraft` carry `sourceType` (enum column) + `sourceId` (UUID column). No database foreign key can enforce this across three different target tables. Referential integrity is enforced by the application layer (use case validates existence before creating a link or draft).

Deleting an evidence source application-cascades both EvidenceLinks and AnalysisDrafts (including proposal children) in the source deletion transaction. AIUsageRecords remain as content-free cost metadata.

`CVPresentation` is an aggregate root with its own table and FK to `professional_profiles.id`; it is not persisted as an owned collection of ProfessionalProfile.

### ProfessionalProfile canonical collections

Experience, Education, Skill, Language, Certification, Project, and ProfileLink use dedicated tables with a required `professional_profile_id` FK. `experience_skills` and `project_skills` are join tables with FKs to the owning entry and canonical Skill; deleting a Skill is blocked while an Experience or Project references it. The application must remove/reassign those references before deleting the Skill.

### JobSource (discriminated union)
Stored as a `sourceKind` discriminator column + `pastedContent` (nullable text) + `storageObjectKey` (nullable text) + `originalFileName` (nullable text) + `mimeType` (nullable text) + `extractedText` (nullable text) on the `JobAnalyses` table.

### EvidenceLink uniqueness
A unique database constraint on `(source_type, source_id, target_study_item_id)` enforces the "at most one link per source–StudyItem pair" invariant.

### One-Pending-draft-per-source
A partial unique index on `(source_type, source_id) WHERE status = 'Pending'` enforces the "at most one Pending draft per source" invariant.

### StudyItem deletion guard
A non-cascade FK from `evidence_links.target_study_item_id` to `study_items.id` ensures that a StudyItem cannot be hard-deleted while any EvidenceLink references it — even if the application-level guard is bypassed.

### Optional InterviewNote → JobAnalysis reference

`interview_notes.job_analysis_id` is a nullable FK with `ON DELETE SET NULL`. Deleting a JobAnalysis preserves the independently useful InterviewNote while removing only the informational association.

### Tags
Stored as a `TEXT[]` PostgreSQL array column on `StudyItems`. Normalisation (trim, lowercase, kebab-case, deduplication) is applied in the domain before persisting. EF Core maps `string[]` to `TEXT[]` via Npgsql.

### YearMonth
Stored as two integer columns (`_year`, `_month`) in the parent table. No native `YearMonth` DB type.

### Proposal collections (AnalysisDraft)
Three separate tables: `suggestion_proposals`, `link_proposals`, `study_item_proposals`, each with an FK to `analysis_drafts.id`. Every row keeps the immutable AI-proposed payload and a separate nullable accepted payload. The `SuggestionProposal` discriminated union (StructuredSuggestion vs AdvisorySuggestion) uses a `kind` column; structured command payloads use JSONB until the command allowlist is finalised.

Proposal status remains Pending while the user reviews a draft in the UI. `ApplyAnalysisDraft` receives the complete decision set and updates all proposal statuses in the same transaction as accepted domain effects and the draft's Applied transition.

### CVPresentation ordered selections

Seven typed join tables preserve order and database referential integrity:

- `cv_presentation_experiences`
- `cv_presentation_educations`
- `cv_presentation_skills`
- `cv_presentation_languages`
- `cv_presentation_certifications`
- `cv_presentation_projects`
- `cv_presentation_profile_links`

Each row contains `cv_presentation_id`, the typed `entry_id`, and `position`. The primary key is `(cv_presentation_id, entry_id)`; a unique constraint on `(cv_presentation_id, position)` prevents duplicate positions. Both IDs have normal FKs. Deleting a CVPresentation cascades to its selections. Deleting a canonical profile entry also cascades only its selection rows; it never deletes a CVPresentation. The application validates that every selected entry belongs to the same ProfessionalProfile referenced by the CVPresentation; this same-profile rule spans tables and cannot be expressed by a simple FK.

### AI usage, budget reservation, and idempotency

`ai_usage_records` is the durable idempotency and cost-control store. `idempotency_key` is unique. Before a provider call, one transaction:

1. transitions stale Reserved rows (older than provider timeout plus safety margin) to Failed;
2. checks for an existing key;
3. calculates daily/monthly usage as Completed `actual_cost` plus active Reserved `reserved_cost`;
4. rejects a call that would exceed either configured budget;
5. inserts a Reserved row with maximum token and cost estimates.

Provider success updates the row to Completed with actual usage, actual cost, and `analysis_draft_id`. Provider failure updates it to Failed and releases unused reservation. A replay of a Completed key returns the existing draft; a Reserved key returns an in-progress conflict; retrying a Failed operation requires a new explicit idempotency key.

## Migration Strategy

- Migrations are applied once per deployment, before the API starts, using a reviewed EF Core migration bundle or an equivalent pre-deploy job. The production API never applies migrations automatically on startup.
- Each migration is reviewed before merging — no auto-generated migrations are applied unreviewed.
- Breaking schema changes (column renames, type changes) are split into additive migrations with a deprecation period.
- Integration tests run against a Testcontainers PostgreSQL instance with migrations applied once per test session; Respawn resets data between tests (not schema).
- The migration job uses a separate privileged migration credential. The running API never receives schema-owner or migration privileges.

## ScoringConfig
A `scoring_config` table with at most one row (no user ID scope — single-user app). The row is absent when defaults apply; it is created on first user save. The application layer never assumes the row exists.

The Application layer resolves the optional row or code defaults into `ScoringWeights` and supplies those weights to `IRankedStudyQueueQuery`. The pure Domain policy owns the formula and validation; it does not load configuration or execute SQL.

## Supabase RLS
Row-Level Security is enabled on all application tables. There are no policies for Supabase `anon` or `authenticated`, so Data API access is denied.

EF Core connects as a dedicated PostgreSQL login role, `commitahead_app`, with only the table/sequence grants needed by the application. Explicit RLS policies grant that role access to the application tables; it is not a superuser, schema owner, or `BYPASSRLS` role. A separate pre-deploy migration credential owns schema changes and is never available to the running API.

The Supabase service-role key is not used by Npgsql. It is reserved for the backend Auth/Storage administrative operations that require it and never reaches the browser.
