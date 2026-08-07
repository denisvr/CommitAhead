-- CommitAhead — grants and Row-Level Security for the Phase 3 business tables.
--
-- Never executed by the running application itself. It IS executed by CI and by
-- backend/scripts/setup-local-db.ps1. Run this AFTER 001_roles.sql, AFTER EF Core migrations have
-- created these tables, and AFTER 002_rls_users.sql/003_rls_phase1.sql/004_rls_phase2.sql (which
-- defines app_current_owner_user_id(), reused here rather than redefined) — ALTER TABLE/GRANT/
-- CREATE POLICY below fail if a table or that function doesn't exist yet.
--
-- This script covers the four Phase 3 tables:
--   job_analyses, interview_notes           (owner_user_id directly)
--   job_requirements, job_gaps              (transitively, via job_analysis_id)
--
-- Isolation model: identical to 003_rls_phase1.sql's/004_rls_phase2.sql's — every policy compares
-- owner_user_id (directly, or transitively through job_analyses for the two child tables) against
-- app_current_owner_user_id(). A NULL comparison is never true, so a request with no owner
-- context matches zero rows on every one of these tables, by construction.
--
-- interview_notes.job_analysis_id is nullable (invariant 19 — the FK behind it is ON DELETE SET
-- NULL, not RLS's concern) but that has no bearing on this script: interview_notes' own
-- owner_user_id column is what every policy here actually checks, exactly like every other
-- directly-scoped table.
--
-- DROP + CREATE, and idempotent GRANT/ALTER statements make this script safe to re-run.

-- commitahead_app never needs DDL rights on these tables — only row-level access, and only what
-- the application actually performs: no TRUNCATE, no REFERENCES/TRIGGER, nothing schema-level.
GRANT SELECT, INSERT, UPDATE, DELETE ON job_analyses TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON job_requirements TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON job_gaps TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON interview_notes TO commitahead_app;

-- ENABLE, not FORCE — see 003_rls_phase1.sql's header comment for the full rationale
-- (commitahead_app is never the table owner, so ENABLE alone already fully restricts it).
-- NO FORCE first so a database that ran an earlier revision with FORCE set gets corrected on
-- re-run, not left with a stale flag ENABLE alone can't clear.
ALTER TABLE job_analyses NO FORCE ROW LEVEL SECURITY;
ALTER TABLE job_analyses ENABLE ROW LEVEL SECURITY;

ALTER TABLE job_requirements NO FORCE ROW LEVEL SECURITY;
ALTER TABLE job_requirements ENABLE ROW LEVEL SECURITY;

ALTER TABLE job_gaps NO FORCE ROW LEVEL SECURITY;
ALTER TABLE job_gaps ENABLE ROW LEVEL SECURITY;

ALTER TABLE interview_notes NO FORCE ROW LEVEL SECURITY;
ALTER TABLE interview_notes ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS owner_isolation ON job_analyses;
CREATE POLICY owner_isolation ON job_analyses
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());

-- job_requirements/job_gaps have no owner_user_id column of their own (model.md: they are
-- one-directional children of JobAnalysis) — isolation is transitive through their parent, exactly
-- like study_reviews' policy in 003_rls_phase1.sql. The subquery is itself subject to
-- job_analyses' own RLS policy above (commitahead_app has no BYPASSRLS), so the owner check
-- effectively runs twice with identical results — harmless, not a gap.
DROP POLICY IF EXISTS owner_isolation ON job_requirements;
CREATE POLICY owner_isolation ON job_requirements
    TO commitahead_app
    USING (
        job_analysis_id IN (
            SELECT id FROM job_analyses
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        job_analysis_id IN (
            SELECT id FROM job_analyses
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON job_gaps;
CREATE POLICY owner_isolation ON job_gaps
    TO commitahead_app
    USING (
        job_analysis_id IN (
            SELECT id FROM job_analyses
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        job_analysis_id IN (
            SELECT id FROM job_analyses
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON interview_notes;
CREATE POLICY owner_isolation ON interview_notes
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());
