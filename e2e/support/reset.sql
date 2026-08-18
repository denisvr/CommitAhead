-- CommitAhead E2E database reset (E2E Foundation Plan; docs/testing/strategy.md §7.4). This file
-- owns ONLY the deterministic SQL transformation — no target selection, no connection details, no
-- Compose knowledge. It is executed exclusively by e2e/scripts/reset-db.mjs, connected as
-- commitahead_migrator (the table owner — RLS here is ENABLE, not FORCE, so an owner connection
-- bypasses row filtering, and only the owner/migrator role holds TRUNCATE; commitahead_app does
-- not, per backend/scripts/database/002_rls_users.sql/004_rls_phase2.sql).
--
-- Truncates every business table and re-seeds the one E2E user. Never drops the schema, the
-- database, or "__EFMigrationsHistory", and never touches RLS policies/grants — TRUNCATE removes
-- rows only; policies, grants, and the RLS ENABLE flag are catalog objects a TRUNCATE cannot
-- affect. RESTART IDENTITY resets serial/identity sequences; CASCADE lets one statement cover all
-- business tables regardless of their FK relationships to each other.

TRUNCATE TABLE
  professional_profiles, certification_entries, education_entries, experience_entries,
  language_entries, profile_links, project_entries, skills,
  cv_presentations
RESTART IDENTITY CASCADE;

-- The one seeded E2E user. supabase_user_id must equal E2E:SupabaseUserId exactly — it is the JWT
-- `sub` claim E2ESessionController mints and the value EnabledUserAuthorizationHandler looks up.
INSERT INTO users (id, supabase_user_id, email, is_enabled, created_at_utc)
VALUES ('11111111-1111-1111-1111-111111111111', 'e2e-user', 'e2e@commitahead.local', true, now())
ON CONFLICT (id) DO UPDATE SET
  supabase_user_id = EXCLUDED.supabase_user_id,
  email = EXCLUDED.email,
  is_enabled = true;
