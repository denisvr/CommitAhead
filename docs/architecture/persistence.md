# CommitAhead — Persistence Strategy

## Approach

EF Core 10 code-first migrations against PostgreSQL (Supabase). The `CommitAheadDbContext` is the single database entry point. Migrations are the authoritative schema source.

## Key Mapping Decisions

### Typed category details (discriminated union)
`StudyItem.details` is a discriminated union of `LeetCodeDetails`, `SystemDesignDetails`, `BehavioralDetails`, and `TheoryDetails`, persisted as a single `jsonb` column (`details`) on `study_items`. The `category` column is the discriminator for domain validation (invariant 6), but the JSON payload is self-describing — it carries its own `kind` tag — because an EF Core `ValueConverter` operates on one column at a time and cannot read a sibling column during (de)serialization.

The Domain layer never references JSON: `StudyItemDetails` and its four subtypes are plain C# types with no serialization attributes. A dedicated converter in `CommitAhead.Infrastructure` (`StudyItemDetailsJsonConverter`, a `System.Text.Json.Serialization.JsonConverter<StudyItemDetails>`) owns the `kind` tag and the mapping to/from each concrete type; the EF `ValueConverter` wraps it and maps the column with `HasColumnType("jsonb")`. This keeps JSON serialization entirely an Infrastructure concern.

No joins are needed to load or rank a StudyItem. Nothing in Phase 1 filters the ranked query by a category-specific detail field — if that need arises later, it is answered by adding a computed/expression index on the `jsonb` column rather than migrating to relational detail tables.

### Ranked-list ordering
The ranked-list query (`IRankedStudyQueueQuery`, implemented by `RankedStudyQueueQuery`) is **not** a SQL-level `ORDER BY` on a computed score column. It loads the owner's Active `StudyItem`s (with their `Reviews`) plus that owner's `EvidenceLink`s in two queries, then computes `Mastery`/`Demand`/`EffectiveScore` and sorts **in memory** in C#, via the same `StudyItem.ComputeMastery()`/`EffectiveScorePolicy` the domain and detail view use — one formula, never a parallel SQL re-implementation that could drift from it. This is deliberate and appropriate at this app's scale (invite-only, so each owner's own Active-item count stays small); ADR-0003 defers denormalisation until a real performance measurement calls for it.

The in-memory sort orders by `EffectiveScore DESC, CreatedAt ASC, Id ASC`. `CreatedAt ASC` is the tiebreak for equal `EffectiveScore` — between two equally-prioritised items, the one waiting longer surfaces first. `Id ASC` is a final tiebreak for the (currently impossible without sub-second `CreatedAt` collisions) case where both are also equal, guaranteeing a fully deterministic order for tests and pagination.

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

- **Tables are owned by EF Core migrations** — they are the single authoritative source for schema (columns, indexes, constraints). Migrations are applied once per deployment, before the API starts, using a reviewed EF Core migration bundle or an equivalent pre-deploy job. The production API never applies migrations automatically on startup.
- **Roles and RLS are owned by the versioned SQL scripts** under `backend/scripts/database/` (`001_roles.sql`, `002_rls_users.sql`, `003_rls_phase1.sql`) — they are the authoritative source for login roles and Row-Level Security policies, not EF Core. This split is deliberate: mixing role/RLS provisioning into EF migrations would make Infrastructure own PostgreSQL-superuser-level concerns it has no business touching (EF Core connects as the least-privileged `commitahead_app` role and cannot grant itself access).
- Locally, `backend/scripts/setup-local-db.ps1` runs all four steps in the correct order as one reproducible command, on every invocation, regardless of whether the Docker volume already existed: it explicitly re-applies `001_roles.sql` itself (idempotent — `IF NOT EXISTS` guards), rather than relying solely on `docker-entrypoint-initdb.d`, which only runs against a brand-new volume and would otherwise silently skip roles on a pre-existing one; then EF migrations; then `002_rls_users.sql`; then `003_rls_phase1.sql`. RLS is never a manually-remembered post-migration step.
- Each migration is reviewed before merging — no auto-generated migrations are applied unreviewed.
- Breaking schema changes (column renames, type changes) are split into additive migrations with a deprecation period.
- Integration tests run against a Testcontainers PostgreSQL instance with migrations applied once per test session; Respawn resets data between tests (not schema).
- The migration job uses a separate privileged migration credential. The running API never receives schema-owner or migration privileges.

## ScoringConfig
A `scoring_config` table with at most one row per `OwnerUserId` (see ADR-0015). A given user's row is absent when defaults apply for them; it is created on that user's first save. The application layer never assumes the row exists.

The Application layer resolves the optional row or code defaults into `ScoringWeights` and supplies those weights to `IRankedStudyQueueQuery`. The pure Domain policy owns the formula and validation; it does not load configuration or execute SQL.

## Supabase RLS
Row-Level Security is enabled AND forced on every table (`002_rls_users.sql` for `users`, `003_rls_phase1.sql` for `study_items`, `study_reviews`, `scoring_config_overrides`, `evidence_links`). FORCE (not just ENABLE) matters here because `commitahead_app` never owns these tables — `commitahead_migrator` does — so FORCE additionally applies the policies to an ad-hoc query run as the table owner or a superuser, instead of that connection silently bypassing RLS by ownership. There are no policies for Supabase `anon` or `authenticated`, so Data API access is denied.

EF Core connects as a dedicated PostgreSQL login role, `commitahead_app`, with only the table/sequence grants needed by the application (`SELECT`/`INSERT`/`UPDATE`/`DELETE`, never DDL). Explicit RLS policies grant that role access to the application tables; it is not a superuser, schema owner, or `BYPASSRLS` role. A separate pre-deploy migration credential owns schema changes and is never available to the running API.

`users` has no owner column — every enabled row is visible to `commitahead_app` (see `002_rls_users.sql`) because the enabled-user check itself must run before any owner is known. The four Phase 1 tables are owner-scoped: each policy's `USING`/`WITH CHECK` compares `owner_user_id` (or, for `study_reviews`, which has no such column of its own, the owning `study_items.owner_user_id` via a subquery on its `study_item_id`) against `app_current_owner_user_id()`, a small `STABLE SQL` function defined in `003_rls_phase1.sql`. A `NULL` comparison is never true, so a connection with no owner context set matches zero rows on every one of these tables by construction, not by a default-deny policy that has to be remembered per table.

The owner value is set **transaction-locally**, never at the session/connection level, so it cannot leak across requests that reuse the same pooled physical connection. `RlsSessionContext` (`CommitAhead.Infrastructure.Persistence`) opens an explicit EF Core transaction, runs `SELECT set_config('app.current_user_id', <ownerUserId>, true)` (the `true` is `is_local`), runs the request's work inside that same transaction, then commits or rolls back — `set_config`'s local scope resets the value automatically at the end of the transaction regardless of outcome. `RlsContextMiddleware` (`CommitAhead.Api.Security`) invokes this for any endpoint whose controller carries `[UsesOwnerScopedData]` (an opt-in marker, mirroring the existing `[SkipCsrf]` pattern) and only once `ICurrentUser` is populated; DB-free endpoints (e.g. `/api/me`) are unaffected and never open a transaction they don't need.

`app_current_owner_user_id()` exists specifically to handle a Postgres quirk with custom GUCs: once a physical connection has called `set_config` for a given custom parameter, `current_setting(name, true)` on that same connection returns `''` (empty string) rather than `NULL` on a later, unrelated transaction that never set it — a bare `current_setting(...)::uuid` cast then throws `22P02 invalid input syntax for type uuid` instead of comparing as "no owner". The function wraps the read in `NULLIF(current_setting('app.current_user_id', true), '')::uuid` so a pooled connection's second-and-later unscoped use degrades to "zero rows", not a hard error. This was found and fixed via `RlsIsolationTests` (`CommitAhead.Infrastructure.Tests.Security`), which runs the real `commitahead_app` role against a dedicated Testcontainers instance bootstrapped the same way `setup-local-db.ps1` bootstraps a real one — not just the Testcontainers-owner connection every other repository test uses.

The Supabase service-role key is not used by Npgsql. It is reserved for the backend Auth/Storage administrative operations that require it and never reaches the browser.
