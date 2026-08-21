# CommitAhead production image — ASP.NET Core API serving the built React SPA from wwwroot
# (backend/src/CommitAhead.Api.csproj's CopyFrontendBuildToPublishOutput target). Build context is
# the repo root, since this needs both frontend/ and backend/ (see docker-compose.prod.yml).
#
# Portable by design (ADR-0021): no cloud-provider base image, no provider-specific tooling. This
# same image runs unchanged in docker-compose.prod.yml today and on whatever hosting platform is
# chosen later.

# ---- Stage 1: build the frontend (frontend/dist is a build input the backend publish target requires) ----
FROM node:24-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Pinned to the exact SDK version backend/global.json requires — the floating "10.0" tag can
# resolve to a later feature band (e.g. 10.0.4xx) that global.json's default rollForward policy
# (latestPatch, same band only) refuses to run, failing `dotnet publish` inside the image with an
# SDK-not-found error even though a *newer* SDK was actually present.
# ---- Stage 2: publish the backend, with the frontend build already in place ----
FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS backend-build
WORKDIR /src
COPY backend/ backend/
COPY --from=frontend-build /src/frontend/dist frontend/dist
WORKDIR /src/backend
RUN dotnet publish src/CommitAhead.Api/CommitAhead.Api.csproj -c Release -o /app/publish

# ---- Stage 3: runtime image — no SDK, no Node, just the published output plus curl for the
#      HEALTHCHECK below (the base image ships neither curl nor wget; verified by actually running
#      the built image and inspecting its HEALTHCHECK failures, not assumed) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Least-privileged runtime user — the base image ships a pre-created "app" user/group (UID/GID
# 1654) for exactly this purpose, so keys/volumes below are owned by a real non-root account.
RUN mkdir -p /keys && chown app:app /keys
USER app

COPY --from=backend-build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Reuses the app's own /api/health endpoint (Features/Health/HealthController.cs, AllowAnonymous) —
# no separate health-check surface to maintain.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "CommitAhead.Api.dll"]
