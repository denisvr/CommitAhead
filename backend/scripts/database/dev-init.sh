#!/bin/bash
# Runs automatically on the local dev Postgres container's first start (docker-entrypoint-initdb.d).
# Substitutes the ${COMMITAHEAD_*_PASSWORD} placeholders in 001_roles.sql from the container's
# environment (populated by docker-compose from backend/.env) and applies it. 002_rls_users.sql
# is NOT run here — it needs the `users` table, which doesn't exist until migrations run.
set -euo pipefail

sed \
  -e "s/\${COMMITAHEAD_MIGRATOR_PASSWORD}/${COMMITAHEAD_MIGRATOR_PASSWORD}/g" \
  -e "s/\${COMMITAHEAD_APP_PASSWORD}/${COMMITAHEAD_APP_PASSWORD}/g" \
  /docker-entrypoint-initdb.d/001_roles.sql.template \
  | psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"
