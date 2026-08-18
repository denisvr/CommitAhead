-- CommitAhead — grants and Row-Level Security for the Phase 2 business tables.
--
-- Never executed by the running application itself. It IS executed by CI and by
-- backend/scripts/setup-local-db.ps1. Run this AFTER 001_roles.sql, AFTER EF Core migrations have
-- created these tables, and AFTER 002_rls_users.sql (which defines app_current_owner_user_id(),
-- reused here rather than redefined) — ALTER TABLE/GRANT/CREATE POLICY below fail if a table or
-- that function doesn't exist yet.
--
-- This script covers the nine Phase 2 tables:
--   professional_profiles, cv_presentations                          (owner_user_id directly)
--   experience_entries, education_entries, skills, language_entries,
--   certification_entries, project_entries, profile_links             (transitively, via
--                                                                       professional_profile_id)
--
-- Isolation model: every policy compares owner_user_id (directly, or transitively through
-- professional_profiles for the seven child tables) against app_current_owner_user_id()
-- (002_rls_users.sql). A NULL comparison is never true, so a request with no owner context
-- matches zero rows on every one of these tables, by construction.
--
-- DROP + CREATE, and idempotent GRANT/ALTER statements make this script safe to re-run.

-- commitahead_app never needs DDL rights on these tables — only row-level access, and only what
-- the application actually performs: no TRUNCATE, no REFERENCES/TRIGGER, nothing schema-level.
GRANT SELECT, INSERT, UPDATE, DELETE ON professional_profiles TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON experience_entries TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON education_entries TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON skills TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON language_entries TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON certification_entries TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON project_entries TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON profile_links TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON cv_presentations TO commitahead_app;

-- ENABLE, not FORCE — see 003_rls_phase1.sql's header comment for the full rationale
-- (commitahead_app is never the table owner, so ENABLE alone already fully restricts it).
-- NO FORCE first so a database that ran an earlier revision with FORCE set gets corrected on
-- re-run, not left with a stale flag ENABLE alone can't clear.
ALTER TABLE professional_profiles NO FORCE ROW LEVEL SECURITY;
ALTER TABLE professional_profiles ENABLE ROW LEVEL SECURITY;

ALTER TABLE experience_entries NO FORCE ROW LEVEL SECURITY;
ALTER TABLE experience_entries ENABLE ROW LEVEL SECURITY;

ALTER TABLE education_entries NO FORCE ROW LEVEL SECURITY;
ALTER TABLE education_entries ENABLE ROW LEVEL SECURITY;

ALTER TABLE skills NO FORCE ROW LEVEL SECURITY;
ALTER TABLE skills ENABLE ROW LEVEL SECURITY;

ALTER TABLE language_entries NO FORCE ROW LEVEL SECURITY;
ALTER TABLE language_entries ENABLE ROW LEVEL SECURITY;

ALTER TABLE certification_entries NO FORCE ROW LEVEL SECURITY;
ALTER TABLE certification_entries ENABLE ROW LEVEL SECURITY;

ALTER TABLE project_entries NO FORCE ROW LEVEL SECURITY;
ALTER TABLE project_entries ENABLE ROW LEVEL SECURITY;

ALTER TABLE profile_links NO FORCE ROW LEVEL SECURITY;
ALTER TABLE profile_links ENABLE ROW LEVEL SECURITY;

ALTER TABLE cv_presentations NO FORCE ROW LEVEL SECURITY;
ALTER TABLE cv_presentations ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS owner_isolation ON professional_profiles;
CREATE POLICY owner_isolation ON professional_profiles
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());

-- The seven canonical child tables have no owner_user_id column of their own (model.md: they are
-- one-directional children of ProfessionalProfile) — isolation is transitive through their
-- parent, exactly like study_reviews' policy in 003_rls_phase1.sql. The subquery is itself
-- subject to professional_profiles' own RLS policy above (commitahead_app has no BYPASSRLS), so
-- the owner check effectively runs twice with identical results — harmless, not a gap.
DROP POLICY IF EXISTS owner_isolation ON experience_entries;
CREATE POLICY owner_isolation ON experience_entries
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON education_entries;
CREATE POLICY owner_isolation ON education_entries
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON skills;
CREATE POLICY owner_isolation ON skills
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON language_entries;
CREATE POLICY owner_isolation ON language_entries
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON certification_entries;
CREATE POLICY owner_isolation ON certification_entries
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON project_entries;
CREATE POLICY owner_isolation ON project_entries
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON profile_links;
CREATE POLICY owner_isolation ON profile_links
    TO commitahead_app
    USING (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        professional_profile_id IN (
            SELECT id FROM professional_profiles
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON cv_presentations;
CREATE POLICY owner_isolation ON cv_presentations
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());
