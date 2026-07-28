-- CommitAhead — Row-Level Security on `users`.
--
-- NOT executed by the application or by CI. Run this AFTER 001_roles.sql and AFTER EF Core
-- migrations have created the `users` table — ALTER TABLE/GRANT/CREATE POLICY below fail if
-- the table doesn't exist yet.
--
-- Row-Level Security is enabled on every application table. Supabase's `anon` and
-- `authenticated` roles get no policies at all, so direct Data API access is denied
-- regardless of RLS — only commitahead_app (used exclusively by the backend) is granted access.
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

GRANT SELECT, INSERT, UPDATE, DELETE ON users TO commitahead_app;

-- The `users` table itself is the identity table the backend resolves `sub` against (ADR-0015),
-- not a user-owned business resource — so commitahead_app gets unrestricted access to it here.
-- Future business-domain tables (StudyItem, ProfessionalProfile, ...) will instead scope their
-- policies by an owner_user_id column, e.g.:
--   CREATE POLICY owner_isolation ON study_items
--     USING (owner_user_id = current_setting('app.current_user_id')::uuid);
-- DROP + CREATE (not CREATE POLICY IF NOT EXISTS, which Postgres doesn't support) makes this
-- script safe to re-run — the reproducible setup flow (backend/scripts/setup-local-db.ps1) may
-- run it more than once against the same database.
DROP POLICY IF EXISTS commitahead_app_full_access ON users;
CREATE POLICY commitahead_app_full_access ON users
    TO commitahead_app
    USING (true)
    WITH CHECK (true);
