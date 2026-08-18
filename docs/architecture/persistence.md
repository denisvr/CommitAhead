# CommitAhead — Persistence Strategy

## Approach

EF Core 10 code-first migrations against PostgreSQL (Supabase). The `CommitAheadDbContext` is the single database entry point. Migrations are the authoritative schema source.

## Key Mapping Decisions

### Polymorphic source reference — removed
Earlier phases gave `EvidenceLink` and `AnalysisDraft` a `sourceType` + `sourceId` polymorphic reference with application-enforced integrity. Both entities, and every table that supported them, were dropped in `20260818163818_DropStudyJobAnalysesInterviewNotesAnalysisDraftsAndAI` (see "Migration History" below). Nothing in the current schema uses this shape.

`CVPresentation` is an aggregate root with its own table and a composite FK to `professional_profiles (id, owner_user_id)` — not a plain single-column FK on `professional_profile_id` alone, and not persisted as an owned collection of ProfessionalProfile. The composite shape (against a `(Id, OwnerUserId)` alternate key on `professional_profiles`) is what makes a cross-owner reference (invariant 29) impossible to persist at all, independent of `CreateCVPresentationUseCase`'s own application-level check.

### ProfessionalProfile canonical collections

Experience, Education, Skill, Language, Certification, Project, and ProfileLink use dedicated tables with a required `professional_profile_id` FK. `ExperienceEntry.SkillIds` and `ProjectEntry.SkillIds` map as a plain `uuid[]` array column — **not** FK-backed `experience_skills`/`project_skills` join tables as originally planned; see ADR-0017 for why and what invariant 21/22 still guarantee at the domain level without a database-level FK on each array element. The application must remove/reassign those references before deleting a Skill; `ProfessionalProfile.ReplaceSkills` enforces this in-memory.

### YearMonth
Stored as a single converted `integer` column (`year * 100 + month`) in the parent table — **not** two separate `_year`/`_month` columns as originally planned; see ADR-0017. No native `YearMonth` DB type, and EF Core cannot constructor-bind a containing entity's parameter to a nested owned/complex sub-object, which a two-column `OwnsOne`/`ComplexProperty` mapping would have required.

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

## Migration Strategy

- **Tables are owned by EF Core migrations** — they are the single authoritative source for schema (columns, indexes, constraints). Migrations are applied once per deployment, before the API starts, using a reviewed EF Core migration bundle or an equivalent pre-deploy job. The production API never applies migrations automatically on startup.
- **Roles and RLS are owned by the versioned SQL scripts** under `backend/scripts/database/` (`001_roles.sql`, `002_rls_users.sql`, `004_rls_phase2.sql`) — they are the authoritative source for login roles and Row-Level Security policies, not EF Core. This split is deliberate: mixing role/RLS provisioning into EF migrations would make Infrastructure own PostgreSQL-superuser-level concerns it has no business touching (EF Core connects as the least-privileged `commitahead_app` role and cannot grant itself access). `004_rls_phase2.sql` covers `professional_profiles`/`cv_presentations` directly by `owner_user_id` and the seven canonical child tables transitively through `professional_profile_id`.
- Locally, `backend/scripts/setup-local-db.ps1` runs all steps in the correct order as one reproducible command, on every invocation, regardless of whether the Docker volume already existed: it explicitly re-applies `001_roles.sql` itself (idempotent — `IF NOT EXISTS` guards), rather than relying solely on `docker-entrypoint-initdb.d`, which only runs against a brand-new volume and would otherwise silently skip roles on a pre-existing one; then EF migrations; then `002_rls_users.sql`; then `004_rls_phase2.sql`. RLS is never a manually-remembered post-migration step.
- Each migration is reviewed before merging — no auto-generated migrations are applied unreviewed.
- Breaking schema changes (column renames, type changes) are split into additive migrations with a deprecation period.
- Integration tests run against a Testcontainers PostgreSQL instance with migrations applied once per test session; Respawn resets data between tests (not schema).
- The migration job uses a separate privileged migration credential. The running API never receives schema-owner or migration privileges.
- **Migration history**: `20260818163818_DropStudyJobAnalysesInterviewNotesAnalysisDraftsAndAI` dropped the thirteen tables that backed the now-removed Study/JobAnalyses/InterviewNotes/AnalysisDrafts/AI features (`study_items`, `study_reviews`, `scoring_config_overrides`, `evidence_links`, `job_analyses`, `job_requirements`, `job_gaps`, `interview_notes`, `analysis_drafts`, `suggestion_proposals`, `link_proposals`, `study_item_proposals`, `ai_usage_records`), alongside the corresponding RLS scripts (formerly `003_rls_phase1.sql` and a Phase 3/4 equivalent) and Storage provisioning script. `professional_profiles`, `cv_presentations`, and their seven canonical child tables were untouched by that migration.

## Supabase RLS
Row-Level Security is **enabled** (not forced) on every remaining table: `002_rls_users.sql` for `users`; `004_rls_phase2.sql` for the nine business tables (`professional_profiles`, `cv_presentations`, and the seven canonical child tables — `experience_entries`, `education_entries`, `skills`, `language_entries`, `certification_entries`, `project_entries`, `profile_links`). ENABLE alone already fully restricts `commitahead_app`, since it never owns these tables — `commitahead_migrator` does — and that is the actual runtime threat model this defends: a compromised or buggy running API, not an ad-hoc query run directly as the table owner or a superuser. FORCE would extend these same policies to that owner/superuser connection too, which is a broader guarantee this app doesn't need and that carries its own operational risk (a migration or admin script connecting as the owner would unexpectedly be row-filtered). There are no policies for Supabase `anon` or `authenticated`, so Data API access is denied regardless.

EF Core connects as a dedicated PostgreSQL login role, `commitahead_app`, with only the table/sequence grants the application actually needs, never DDL: `SELECT`/`INSERT`/`UPDATE`/`DELETE` on the nine business tables, but **`SELECT` only on `users`** — provisioning (creating or enabling a user) is a privileged operation the running application can never perform itself, by grant, not just by convention (see `docs/tbd.md` "Invited-user provisioning"). Explicit RLS policies grant `commitahead_app` access to the application tables; it is not a superuser, schema owner, or `BYPASSRLS` role. A separate pre-deploy migration credential owns schema changes and is never available to the running API.

`users` has no owner column — every enabled row is visible (read-only) to `commitahead_app` (see `002_rls_users.sql`) because the enabled-user check itself must run before any owner is known. `professional_profiles` and `cv_presentations` are owner-scoped directly: each policy's `USING`/`WITH CHECK` compares `owner_user_id` against `app_current_owner_user_id()`, a small `STABLE SQL` function defined in `004_rls_phase2.sql`. A `NULL` comparison is never true, so a connection with no owner context set matches zero rows on either table by construction, not by a default-deny policy that has to be remembered per table. The seven canonical child tables have no `owner_user_id` column of their own — they are scoped transitively: each policy's `USING`/`WITH CHECK` resolves `professional_profile_id` against a subquery over `professional_profiles` filtered by `owner_user_id = app_current_owner_user_id()`.

The owner value is set **transaction-locally**, never at the session/connection level, so it cannot leak across requests that reuse the same pooled physical connection. `RlsSessionContext` (`CommitAhead.Infrastructure.Persistence`) opens an explicit EF Core transaction, runs `SELECT set_config('app.current_user_id', <ownerUserId>, true)` (the `true` is `is_local`), runs the request's work inside that same transaction, then commits or rolls back — `set_config`'s local scope resets the value automatically at the end of the transaction regardless of outcome. `RlsTransactionActionFilter` (`CommitAhead.Api.Filters`) invokes this for any action carrying `[UsesOwnerScopedData]` (an opt-in marker, mirroring the existing `[SkipCsrf]` pattern, applied per-action — not always per-controller) and only once `ICurrentUser` is populated. It is a global MVC **action filter**, not middleware: the commit happens as part of the action stage, strictly before the result stage writes any response bytes, so a client is never handed a "success" response for a write that has not actually persisted — a risk a middleware wrapping the whole request pipeline (including response writing) would carry. DB-free endpoints (e.g. `/api/me`) are unaffected and never open a transaction they don't need.

`app_current_owner_user_id()` exists specifically to handle a Postgres quirk with custom GUCs: once a physical connection has called `set_config` for a given custom parameter, `current_setting(name, true)` on that same connection returns `''` (empty string) rather than `NULL` on a later, unrelated transaction that never set it — a bare `current_setting(...)::uuid` cast then throws `22P02 invalid input syntax for type uuid` instead of comparing as "no owner". The function wraps the read in `NULLIF(current_setting('app.current_user_id', true), '')::uuid` so a pooled connection's second-and-later unscoped use degrades to "zero rows", not a hard error. This was found and fixed via `RlsIsolationTests` (`CommitAhead.Infrastructure.Tests.Security`), which runs the real `commitahead_app` role against a dedicated Testcontainers instance bootstrapped the same way `setup-local-db.ps1` bootstraps a real one — not just the Testcontainers-owner connection every other repository test uses.

The Supabase service-role key is not used by Npgsql. It is reserved for backend Auth-session administration and never reaches the browser (ADR-0006).
