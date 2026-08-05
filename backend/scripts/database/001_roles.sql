-- CommitAhead — dedicated PostgreSQL login roles.
--
-- Never executed by the running application itself. It IS executed by CI: the Infrastructure.Tests
-- integration suite (RlsIsolationTests, RlsHttpIsolationTests) applies this script against a
-- disposable Testcontainers database for every run, and backend/scripts/setup-local-db.ps1 applies
-- it for local dev. Documentation-as-SQL matching docs/architecture/persistence.md ("Supabase RLS")
-- and ADR-0007/ADR-0015 for the real Supabase project, which still needs it run manually.
--
-- Safe to run before any tables exist (schema-level only) — run this FIRST, as the
-- project's postgres/superuser role, then apply EF Core migrations, then run
-- 002_rls_users.sql (which needs the `users` table to already exist).
--
-- Two roles:
--   commitahead_migrator — owns schema changes (DDL, migrations). Never used by the running API.
--   commitahead_app      — least-privileged runtime role used by Npgsql/EF Core. No DDL rights,
--                           not a superuser, not BYPASSRLS.
--
-- ${COMMITAHEAD_MIGRATOR_PASSWORD} / ${COMMITAHEAD_APP_PASSWORD} are placeholders: substitute
-- real values before running against Supabase (never commit real values anywhere). Local dev
-- substitutes them automatically from backend/.env via docker-compose (see README.md at the repo root).

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'commitahead_migrator') THEN
        CREATE ROLE commitahead_migrator LOGIN PASSWORD '${COMMITAHEAD_MIGRATOR_PASSWORD}' NOSUPERUSER NOCREATEDB NOCREATEROLE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'commitahead_app') THEN
        CREATE ROLE commitahead_app LOGIN PASSWORD '${COMMITAHEAD_APP_PASSWORD}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;
END
$$;

-- commitahead_migrator owns the schema so EF Core migrations can create/alter tables.
GRANT CREATE, USAGE ON SCHEMA public TO commitahead_migrator;

-- commitahead_app only ever reads/writes rows in already-created tables; it cannot alter schema.
GRANT USAGE ON SCHEMA public TO commitahead_app;
