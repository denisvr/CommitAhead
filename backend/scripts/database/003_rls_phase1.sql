-- CommitAhead — grants and Row-Level Security for the Phase 1 business tables.
--
-- NOT executed by the application or by CI. Run this AFTER 001_roles.sql, AFTER EF Core migrations
-- have created these tables, and AFTER 002_rls_users.sql — ALTER TABLE/GRANT/CREATE POLICY below
-- fail if a table doesn't exist yet.
--
-- 002_rls_users.sql covers `users` (the identity table, unconditionally open to commitahead_app —
-- see its own header comment for why). This script covers the four owner-scoped Phase 1 tables:
--   study_items, study_reviews, scoring_config_overrides, evidence_links
--
-- Isolation model: every policy compares owner_user_id (directly, or transitively for
-- study_reviews via its parent study_item) against app_current_owner_user_id(), defined below.
-- A NULL comparison is never true, so a request with no owner context matches zero rows on every
-- one of these tables, by construction — not by a default-deny policy that has to be remembered.
--
-- The application sets the underlying value once per request, transaction-locally, via
-- RlsSessionContext.RunInOwnerScopeAsync (SELECT set_config('app.current_user_id', <id>, true)) —
-- never a session/connection-level SET, which could leak into a later request that reuses the
-- same pooled physical connection. See docs/architecture/persistence.md "Supabase RLS".
--
-- DROP + CREATE (not CREATE POLICY IF NOT EXISTS, which Postgres doesn't support), CREATE OR
-- REPLACE, and idempotent GRANT/ALTER statements make this script safe to re-run — the
-- reproducible setup flow (backend/scripts/setup-local-db.ps1) may run it more than once against
-- the same database.

-- Postgres custom GUCs (unlike built-in ones) behave subtly once touched by set_config(...,
-- true) in a session: after that transaction ends the value resets, but the parameter itself
-- stays "known" to the session as an EMPTY STRING rather than becoming undefined again —
-- current_setting(..., true) then returns '' instead of NULL, and ''::uuid raises a hard
-- 22P02 error rather than comparing as "no owner". A pooled connection previously used by a
-- scoped request hits exactly this on its next unscoped use, so every policy goes through this
-- helper instead of casting current_setting(...) directly.
CREATE OR REPLACE FUNCTION app_current_owner_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
AS $$
    SELECT NULLIF(current_setting('app.current_user_id', true), '')::uuid;
$$;

GRANT EXECUTE ON FUNCTION app_current_owner_user_id() TO commitahead_app;

-- commitahead_app never needs DDL rights on these tables — only row-level access, and only what
-- the application actually performs: no TRUNCATE, no REFERENCES/TRIGGER, nothing schema-level.
GRANT SELECT, INSERT, UPDATE, DELETE ON study_items TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON study_reviews TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON scoring_config_overrides TO commitahead_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON evidence_links TO commitahead_app;

-- ENABLE, not FORCE: commitahead_app is never the table owner (commitahead_migrator is), so
-- ENABLE alone already fully restricts it — that is the actual runtime threat model (a compromised
-- or buggy running API), not an ad-hoc query run directly as commitahead_migrator or a superuser.
-- FORCE would also apply these policies to that owner/superuser connection, which is a different,
-- broader guarantee this app doesn't need and that carries its own operational risk (a migration
-- or admin script connecting as the owner would unexpectedly be row-filtered too).
ALTER TABLE study_items ENABLE ROW LEVEL SECURITY;

ALTER TABLE study_reviews ENABLE ROW LEVEL SECURITY;

ALTER TABLE scoring_config_overrides ENABLE ROW LEVEL SECURITY;

ALTER TABLE evidence_links ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS owner_isolation ON study_items;
CREATE POLICY owner_isolation ON study_items
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());

-- study_reviews has no owner_user_id column of its own (model.md describes it as a
-- one-directional child of StudyItem) — isolation is transitive through its parent. The subquery
-- is itself subject to study_items' own RLS policy above (commitahead_app has no BYPASSRLS), so
-- the owner check effectively runs twice with identical results — harmless, not a gap.
DROP POLICY IF EXISTS owner_isolation ON study_reviews;
CREATE POLICY owner_isolation ON study_reviews
    TO commitahead_app
    USING (
        study_item_id IN (
            SELECT id FROM study_items
            WHERE owner_user_id = app_current_owner_user_id()
        )
    )
    WITH CHECK (
        study_item_id IN (
            SELECT id FROM study_items
            WHERE owner_user_id = app_current_owner_user_id()
        )
    );

DROP POLICY IF EXISTS owner_isolation ON scoring_config_overrides;
CREATE POLICY owner_isolation ON scoring_config_overrides
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());

DROP POLICY IF EXISTS owner_isolation ON evidence_links;
CREATE POLICY owner_isolation ON evidence_links
    TO commitahead_app
    USING (owner_user_id = app_current_owner_user_id())
    WITH CHECK (owner_user_id = app_current_owner_user_id());
