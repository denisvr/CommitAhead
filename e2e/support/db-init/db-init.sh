#!/bin/bash
# CommitAhead E2E db-init — one-shot: roles -> EF migration bundle -> RLS (E2E Foundation Plan).
# set -euo pipefail plus -v ON_ERROR_STOP=1 on every psql invocation means any failure here exits
# non-zero immediately, so `depends_on: db-init: condition: service_completed_successfully`
# (docker-compose.e2e.yml) never lets `app` start against a half-migrated database. This container
# never runs a second time against the same data directory in the normal lifecycle — the E2E
# database has no persistent volume — but 001_roles.sql's own idempotent CREATE ROLE guard and
# 002/004's DROP POLICY IF EXISTS/CREATE POLICY pattern make a manual re-run safe regardless.
set -euo pipefail

: "${PGHOST:?PGHOST is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${COMMITAHEAD_MIGRATOR_PASSWORD:?COMMITAHEAD_MIGRATOR_PASSWORD is required}"
: "${COMMITAHEAD_APP_PASSWORD:?COMMITAHEAD_APP_PASSWORD is required}"

export PGPASSWORD="$POSTGRES_PASSWORD"

echo "db-init: applying roles (001_roles.sql) as postgres..."
sed \
  -e "s/\${COMMITAHEAD_MIGRATOR_PASSWORD}/${COMMITAHEAD_MIGRATOR_PASSWORD}/g" \
  -e "s/\${COMMITAHEAD_APP_PASSWORD}/${COMMITAHEAD_APP_PASSWORD}/g" \
  /sql/001_roles.sql \
  | psql -v ON_ERROR_STOP=1 -h "$PGHOST" -U postgres -d "$PGDATABASE"

echo "db-init: running the EF migration bundle as commitahead_migrator..."
/efbundle --connection "Host=$PGHOST;Port=5432;Database=$PGDATABASE;Username=commitahead_migrator;Password=$COMMITAHEAD_MIGRATOR_PASSWORD"

for script in 002_rls_users.sql 004_rls_phase2.sql; do
  echo "db-init: applying $script as postgres..."
  psql -v ON_ERROR_STOP=1 -h "$PGHOST" -U postgres -d "$PGDATABASE" -f "/sql/$script"
done

echo "db-init: roles -> migrations -> RLS complete."
