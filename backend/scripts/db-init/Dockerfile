# CommitAhead dev db-init — one-shot initializer for the fully-containerized dev environment
# (ADR-0022): roles -> EF migration bundle -> RLS, so `docker compose up` alone gets a freshly
# migrated database with no .NET SDK on the host at all. Deliberately a separate copy from
# e2e/support/db-init/ rather than a shared one: that directory is E2E-owned (docs/testing/
# strategy.md SS7.11) and this dev stack must stay a genuinely separate environment from it, per
# ADR-0021's own "do not mix local runtimes" rule — this file happens to do the same kind of thing,
# not the same thing for the same reason. Build context is the repo root (see docker-compose.dev.yml),
# matching every other Dockerfile's own convention, since this needs backend/.
#
# The migration bundle uses the SAME runtime as backend/scripts/build-migration-bundle.ps1 and
# e2e/support/db-init/Dockerfile (--self-contained --runtime linux-x64) — no new musl/native-
# dependency risk solely for a one-shot initializer. The final stage is therefore postgres:17
# (Debian, glibc), not postgres:17-alpine — Alpine's musl libc cannot run a linux-x64 (glibc)
# self-contained .NET bundle. The dev database server itself (backend/docker-compose.yml's db
# service) stays on postgres:17-alpine; only this short-lived initializer needs glibc.

# ---- Stage 1: build a self-contained linux-x64 EF migration bundle ----
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS bundle-build
WORKDIR /src
COPY backend/ backend/
WORKDIR /src/backend

# Explicit tool path (not a global install) so invocation below is unambiguous and does not
# depend on PATH/profile resolution inside the image.
RUN dotnet tool install dotnet-ef --version 10.0.10 --tool-path /dotnet-tools

RUN dotnet restore src/CommitAhead.Api/CommitAhead.Api.csproj --runtime linux-x64

RUN /dotnet-tools/dotnet-ef migrations bundle \
    --project src/CommitAhead.Infrastructure \
    --startup-project src/CommitAhead.Api \
    --self-contained \
    --runtime linux-x64 \
    --force \
    --output /out/efbundle

# ---- Stage 2: postgres:17 (Debian) — glibc for the bundle above; psql 17 matches the dev db
#      service's own major version, so no cross-version client/server surprises ----
FROM postgres:17

COPY --from=bundle-build /out/efbundle /efbundle
RUN chmod +x /efbundle

COPY backend/scripts/database/001_roles.sql /sql/001_roles.sql
COPY backend/scripts/database/002_rls_users.sql /sql/002_rls_users.sql
COPY backend/scripts/database/004_rls_phase2.sql /sql/004_rls_phase2.sql

COPY backend/scripts/db-init/db-init.sh /db-init.sh
RUN chmod +x /db-init.sh

ENTRYPOINT ["/db-init.sh"]
