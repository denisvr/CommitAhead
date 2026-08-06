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

`CVPresentation` is an aggregate root with its own table and a composite FK to `professional_profiles (id, owner_user_id)` — not a plain single-column FK on `professional_profile_id` alone, and not persisted as an owned collection of ProfessionalProfile. The composite shape (against a `(Id, OwnerUserId)` alternate key on `professional_profiles`) is what makes a cross-owner reference (invariant 29) impossible to persist at all, independent of `CreateCVPresentationUseCase`'s own application-level check.

### ProfessionalProfile canonical collections

Experience, Education, Skill, Language, Certification, Project, and ProfileLink use dedicated tables with a required `professional_profile_id` FK. `ExperienceEntry.SkillIds` and `ProjectEntry.SkillIds` map as a plain `uuid[]` array column — **not** FK-backed `experience_skills`/`project_skills` join tables as originally planned; see ADR-0017 for why and what invariant 21/22 still guarantee at the domain level without a database-level FK on each array element. The application must remove/reassign those references before deleting a Skill; `ProfessionalProfile.ReplaceSkills` enforces this in-memory.

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
Stored as a single converted `integer` column (`year * 100 + month`) in the parent table — **not** two separate `_year`/`_month` columns as originally planned; see ADR-0017. No native `YearMonth` DB type, and EF Core cannot constructor-bind a containing entity's parameter to a nested owned/complex sub-object, which a two-column `OwnsOne`/`ComplexProperty` mapping would have required.

### Proposal collections (AnalysisDraft)
Three separate tables: `suggestion_proposals`, `link_proposals`, `study_item_proposals`, each with an FK to `analysis_drafts.id`. Every row keeps the immutable AI-proposed payload and a separate nullable accepted payload. The `SuggestionProposal` discriminated union (StructuredSuggestion vs AdvisorySuggestion) uses a `kind` column; structured command payloads use JSONB until the command allowlist is finalised.

Proposal status remains Pending while the user reviews a draft in the UI. `ApplyAnalysisDraft` receives the complete decision set and updates all proposal statuses in the same transaction as accepted domain effects and the draft's Applied transition.

### CVPresentation ordered selections

Each of the seven selections (Experience, Education, Skill, Language, Certification, Project,
ProfileLink) maps as a plain `uuid[]` array column on `cv_presentations` — **not** seven typed,
FK-backed join tables as originally planned; see ADR-0017 for why (the same EF Core
constructor-binding wall as `SkillIds` above) and what invariant 24 still guarantees without a
database-level FK on each array element. Array order **is** position — there is no separate
`position` value to keep in sync, and therefore no separate uniqueness constraint on it either.
Deleting a canonical profile entry removes its ID from any presentation's selection array
(`DanglingSelectionCleanup`, run from every `ProfessionalProfile.Replace*UseCase`); it never
deletes a CVPresentation. The application validates that every selected entry belongs to the same
ProfessionalProfile referenced by the CVPresentation (invariant 23); this same-profile rule spans
two aggregates and cannot be expressed by a simple FK, unlike the CVPresentation→ProfessionalProfile
same-owner rule above, which the composite FK does enforce at the database level.

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
- **Roles and RLS are owned by the versioned SQL scripts** under `backend/scripts/database/` (`001_roles.sql`, `002_rls_users.sql`, `003_rls_phase1.sql`, `004_rls_phase2.sql`) — they are the authoritative source for login roles and Row-Level Security policies, not EF Core. This split is deliberate: mixing role/RLS provisioning into EF migrations would make Infrastructure own PostgreSQL-superuser-level concerns it has no business touching (EF Core connects as the least-privileged `commitahead_app` role and cannot grant itself access). `004_rls_phase2.sql` covers `professional_profiles`/`cv_presentations` directly by `owner_user_id` and the seven canonical child tables transitively through `professional_profile_id`, mirroring `003_rls_phase1.sql`'s `study_reviews` pattern for a child table with no `owner_user_id` column of its own.
- Locally, `backend/scripts/setup-local-db.ps1` runs all steps in the correct order as one reproducible command, on every invocation, regardless of whether the Docker volume already existed: it explicitly re-applies `001_roles.sql` itself (idempotent — `IF NOT EXISTS` guards), rather than relying solely on `docker-entrypoint-initdb.d`, which only runs against a brand-new volume and would otherwise silently skip roles on a pre-existing one; then EF migrations; then `002_rls_users.sql`; then `003_rls_phase1.sql`; then `004_rls_phase2.sql`. RLS is never a manually-remembered post-migration step.
- Each migration is reviewed before merging — no auto-generated migrations are applied unreviewed.
- Breaking schema changes (column renames, type changes) are split into additive migrations with a deprecation period.
- Integration tests run against a Testcontainers PostgreSQL instance with migrations applied once per test session; Respawn resets data between tests (not schema).
- The migration job uses a separate privileged migration credential. The running API never receives schema-owner or migration privileges.

## ScoringConfig
A `scoring_config` table with at most one row per `OwnerUserId` (see ADR-0015). A given user's row is absent when defaults apply for them; it is created on that user's first save. The application layer never assumes the row exists.

The Application layer resolves the optional row or code defaults into `ScoringWeights` and supplies those weights to `IRankedStudyQueueQuery`. The pure Domain policy owns the formula and validation; it does not load configuration or execute SQL.

## Supabase RLS
Row-Level Security is **enabled** (not forced) on every table: `002_rls_users.sql` for `users`; `003_rls_phase1.sql` for the four Phase 1 tables (`study_items`, `study_reviews`, `scoring_config_overrides`, `evidence_links`); `004_rls_phase2.sql` for the nine Phase 2 tables (`professional_profiles`, `cv_presentations`, and the seven canonical child tables — `experience_entries`, `education_entries`, `skills`, `language_entries`, `certification_entries`, `project_entries`, `profile_links`). ENABLE alone already fully restricts `commitahead_app`, since it never owns these tables — `commitahead_migrator` does — and that is the actual runtime threat model this defends: a compromised or buggy running API, not an ad-hoc query run directly as the table owner or a superuser. FORCE would extend these same policies to that owner/superuser connection too, which is a broader guarantee this app doesn't need and that carries its own operational risk (a migration or admin script connecting as the owner would unexpectedly be row-filtered). There are no policies for Supabase `anon` or `authenticated`, so Data API access is denied regardless.

EF Core connects as a dedicated PostgreSQL login role, `commitahead_app`, with only the table/sequence grants the application actually needs, never DDL: `SELECT`/`INSERT`/`UPDATE`/`DELETE` on the four Phase 1 tables and the nine Phase 2 tables, but **`SELECT` only on `users`** — provisioning (creating or enabling a user) is a privileged operation the running application can never perform itself, by grant, not just by convention (see `docs/tbd.md` "Invited-user provisioning"). Explicit RLS policies grant `commitahead_app` access to the application tables; it is not a superuser, schema owner, or `BYPASSRLS` role. A separate pre-deploy migration credential owns schema changes and is never available to the running API.

`users` has no owner column — every enabled row is visible (read-only) to `commitahead_app` (see `002_rls_users.sql`) because the enabled-user check itself must run before any owner is known. The four Phase 1 tables are owner-scoped: each policy's `USING`/`WITH CHECK` compares `owner_user_id` (or, for `study_reviews`, which has no such column of its own, the owning `study_items.owner_user_id` via a subquery on its `study_item_id`) against `app_current_owner_user_id()`, a small `STABLE SQL` function defined in `003_rls_phase1.sql`. A `NULL` comparison is never true, so a connection with no owner context set matches zero rows on every one of these tables by construction, not by a default-deny policy that has to be remembered per table.

The nine Phase 2 tables (`004_rls_phase2.sql`) follow the same construction, reusing `app_current_owner_user_id()`. `professional_profiles` and `cv_presentations` are owner-scoped directly, exactly like the Phase 1 tables: `owner_user_id` compared against `app_current_owner_user_id()`. The seven canonical child tables have no `owner_user_id` column of their own — they are scoped transitively, the same way `study_reviews` is: each policy's `USING`/`WITH CHECK` resolves `professional_profile_id` against a subquery over `professional_profiles` filtered by `owner_user_id = app_current_owner_user_id()`.

The owner value is set **transaction-locally**, never at the session/connection level, so it cannot leak across requests that reuse the same pooled physical connection. `RlsSessionContext` (`CommitAhead.Infrastructure.Persistence`) opens an explicit EF Core transaction, runs `SELECT set_config('app.current_user_id', <ownerUserId>, true)` (the `true` is `is_local`), runs the request's work inside that same transaction, then commits or rolls back — `set_config`'s local scope resets the value automatically at the end of the transaction regardless of outcome. `RlsTransactionActionFilter` (`CommitAhead.Api.Filters`) invokes this for any action whose controller carries `[UsesOwnerScopedData]` (an opt-in marker, mirroring the existing `[SkipCsrf]` pattern) and only once `ICurrentUser` is populated. It is a global MVC **action filter**, not middleware: the commit happens as part of the action stage, strictly before the result stage writes any response bytes, so a client is never handed a "success" response for a write that has not actually persisted — a risk a middleware wrapping the whole request pipeline (including response writing) would carry. DB-free endpoints (e.g. `/api/me`) are unaffected and never open a transaction they don't need.

`app_current_owner_user_id()` exists specifically to handle a Postgres quirk with custom GUCs: once a physical connection has called `set_config` for a given custom parameter, `current_setting(name, true)` on that same connection returns `''` (empty string) rather than `NULL` on a later, unrelated transaction that never set it — a bare `current_setting(...)::uuid` cast then throws `22P02 invalid input syntax for type uuid` instead of comparing as "no owner". The function wraps the read in `NULLIF(current_setting('app.current_user_id', true), '')::uuid` so a pooled connection's second-and-later unscoped use degrades to "zero rows", not a hard error. This was found and fixed via `RlsIsolationTests` (`CommitAhead.Infrastructure.Tests.Security`), which runs the real `commitahead_app` role against a dedicated Testcontainers instance bootstrapped the same way `setup-local-db.ps1` bootstraps a real one — not just the Testcontainers-owner connection every other repository test uses.

The Supabase service-role key is not used by Npgsql. It is reserved for the backend Auth/Storage administrative operations that require it and never reaches the browser.
