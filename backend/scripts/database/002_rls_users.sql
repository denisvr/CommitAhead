-- CommitAhead — Row-Level Security on `users`.
--
-- Never executed by the running application itself. It IS executed by CI (see 001_roles.sql's
-- header) and by backend/scripts/setup-local-db.ps1. Run this AFTER 001_roles.sql and AFTER EF
-- Core migrations have created the `users` table — ALTER TABLE/GRANT/CREATE POLICY below fail if
-- the table doesn't exist yet.
--
-- Row-Level Security is enabled on every application table. Supabase's `anon` and
-- `authenticated` roles get no policies at all, so direct Data API access is denied
-- regardless of RLS — only commitahead_app (used exclusively by the backend) is granted access.
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- SELECT only: the running application resolves `sub` -> User for every request (ADR-0015) but
-- never creates, enables, or edits a user row itself — provisioning a new invited user is a
-- privileged, out-of-band operation (see docs/tbd.md "Invited-user provisioning"), run with a
-- different, more-trusted credential than the one the API connects with. If the app is ever
-- compromised at the request level, it still cannot create or enable an account for itself.
--
-- REVOKE first: a database that already ran an earlier revision of this script (before this
-- restriction existed) has commitahead_app holding INSERT/UPDATE/DELETE on users — GRANT SELECT
-- alone only adds a privilege, it never takes back ones already held, so the REVOKE is what
-- actually corrects that state on re-run, not just the GRANT.
REVOKE INSERT, UPDATE, DELETE ON users FROM commitahead_app;
GRANT SELECT ON users TO commitahead_app;

-- The `users` table itself is the identity table the backend resolves `sub` against (ADR-0015),
-- not a user-owned business resource — so commitahead_app can read every row here (there is no
-- owner_user_id column to scope by), but only SELECT, per the grant above. FOR SELECT makes that
-- explicit at the policy level too, not just via the grant. Future business-domain tables
-- (StudyItem, ProfessionalProfile, ...) instead scope their policies by an owner_user_id column,
-- e.g.:
--   CREATE POLICY owner_isolation ON study_items
--     USING (owner_user_id = current_setting('app.current_user_id')::uuid);
-- DROP + CREATE (not CREATE POLICY IF NOT EXISTS, which Postgres doesn't support) makes this
-- script safe to re-run — the reproducible setup flow (backend/scripts/setup-local-db.ps1) may
-- run it more than once against the same database.
DROP POLICY IF EXISTS commitahead_app_full_access ON users;
DROP POLICY IF EXISTS commitahead_app_read_access ON users;
CREATE POLICY commitahead_app_read_access ON users
    FOR SELECT
    TO commitahead_app
    USING (true);
