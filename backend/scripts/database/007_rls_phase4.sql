-- CommitAhead — grants and Row-Level Security for the Phase 4 AnalysisDraft/AIUsageRecord tables.
--
-- Never executed by the running application itself. It IS executed by CI and by
-- backend/scripts/setup-local-db.ps1. Run this AFTER 001_roles.sql, AFTER EF Core migrations have
-- created these tables, and AFTER 002_rls_users.sql (which defines app_current_owner_user_id(),
-- reused here rather than redefined) — ALTER TABLE/GRANT/CREATE POLICY below fail if a table or
-- that function doesn't exist yet.
--
-- This script covers the five Phase 4 tables:
--   analysis_drafts, ai_usage_records                          (owner_user_id directly)
--   suggestion_proposals, link_proposals, study_item_proposals  (transitively, via analysis_draft_id)
--
-- Isolation model: identical to 003_rls_phase1.sql's/004_rls_phase2.sql's/005_rls_phase3.sql's —
-- every policy compares owner_user_id (directly, or transitively through analysis_drafts for the
-- three proposal tables) against app_current_owner_user_id(). A NULL comparison is never true, so
-- a request with no owner context matches zero rows on every one of these tables, by construction.
--
-- DROP + CREATE, and idempotent GRANT/ALTER statements make this script safe to re-run.

-- commitahead_app never needs DDL rights on these tables — only row-level access, and only what
-- the application actually performs: no TRUNCATE, no REFERENCES/TRIGGER, nothing schema-level.
GRANT SELECT, INSERT, UPDATE, DELETE ON analysis_drafts TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON suggestion_proposals TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON link_proposals TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON study_item_proposals TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ai_usage_records TO commitahead_app;

-- ENABLE, not FORCE — see 003_rls_phase1.sql's header comment for the full rationale
-- (commitahead_app is never the table owner, so ENABLE alone already fully restricts it).
-- NO FORCE first so a database that ran an earlier revision with FORCE set gets corrected on
-- re-run, not left with a stale flag ENABLE alone can't clear.
ALTER TABLE analysis_drafts NO FORCE ROW LEVEL SECURITY;
ALTER TABLE analysis_drafts ENABLE ROW LEVEL SECURITY;

ALTER TABLE suggestion_proposals NO FORCE ROW LEVEL SECURITY;
ALTER TABLE suggestion_proposals ENABLE ROW LEVEL SECURITY;

ALTER TABLE link_proposals NO FORCE ROW LEVEL SECURITY;
ALTER TABLE link_proposals ENABLE ROW LEVEL SECURITY;

ALTER TABLE study_item_proposals NO FORCE ROW LEVEL SECURITY;
ALTER TABLE study_item_proposals ENABLE ROW LEVEL SECURITY;

ALTER TABLE ai_usage_records NO FORCE ROW LEVEL SECURITY;
ALTER TABLE ai_usage_records ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS owner_isolation ON analysis_drafts;
CREATE POLICY owner_isolation ON analysis_drafts
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());

-- suggestion_proposals/link_proposals/study_item_proposals have no owner_user_id column of their
-- own (model.md: they are one-directional children of AnalysisDraft) — isolation is transitive
-- through their parent, exactly like job_requirements'/job_gaps' policies in 005_rls_phase3.sql.
-- The subquery is itself subject to analysis_drafts' own RLS policy above (commitahead_app has no
-- BYPASSRLS), so the owner check effectively runs twice with identical results — harmless, not a gap.
DROP POLICY IF EXISTS owner_isolation ON suggestion_proposals;
CREATE POLICY owner_isolation ON suggestion_proposals
    TO commitahead_app
    USING (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON link_proposals;
CREATE POLICY owner_isolation ON link_proposals
    TO commitahead_app
    USING (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON study_item_proposals;
CREATE POLICY owner_isolation ON study_item_proposals
    TO commitahead_app
    USING (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        analysis_draft_id IN (
            SELECT id FROM analysis_drafts
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON ai_usage_records;
CREATE POLICY owner_isolation ON ai_usage_records
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());
