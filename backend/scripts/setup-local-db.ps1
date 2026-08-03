# CommitAhead - reproducible local dev database bootstrap: roles -> migrations -> RLS(users) -> RLS(Phase 1).
#
# EF Core migrations are the authoritative source for tables; this script and the versioned SQL
# under scripts/database/ are the authoritative source for roles and RLS (see
# docs/architecture/persistence.md "Migration Strategy"). Never touches the real Supabase
# Postgres - this only ever targets the local Docker Postgres in docker-compose.yml.
#
# Runs roles -> migrations -> RLS on EVERY invocation, not just against a fresh Docker volume:
# docker-entrypoint-initdb.d (001_roles.sql via dev-init.sh) only runs the first time Postgres
# initializes an empty data directory, so it silently does nothing on a pre-existing volume. This
# script re-applies 001_roles.sql itself every time - it is idempotent (IF NOT EXISTS guards), so
# roles end up present regardless of the volume's prior state.
#
# Safe to re-run: docker compose up is idempotent, 001_roles.sql only creates roles that don't
# already exist, EF migrations only apply what's pending, and 002_rls_users.sql/003_rls_phase1.sql
# both drop+recreate their policies instead of failing on a second run.
#
# NOTE: keep this file plain ASCII. Windows PowerShell 5.1 misparses non-ASCII characters (e.g.
# an em dash) in a .ps1 file that has no UTF-8 BOM, producing confusing "missing terminator"
# syntax errors far from the actual offending line.

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
Push-Location $backendDir

# Restored (or removed, if it wasn't set before) in `finally` - this variable carries the real
# migrator password for the duration of the migration step only, never left behind in the calling
# session's environment.
$originalMigrationConnection = $env:COMMITAHEAD_MIGRATION_CONNECTION

try {
    if (-not (Test-Path ".env")) {
        Write-Error ".env not found in backend/ - copy .env.example to .env and set real passwords first."
        exit 1
    }

    $envValues = @{}
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)\s*=\s*(.*)\s*$') {
            $envValues[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    $requiredVars = @("POSTGRES_PASSWORD", "COMMITAHEAD_MIGRATOR_PASSWORD", "COMMITAHEAD_APP_PASSWORD")
    $missingVars = $requiredVars | Where-Object { [string]::IsNullOrWhiteSpace($envValues[$_]) }
    if ($missingVars.Count -gt 0) {
        Write-Error "backend/.env is missing required value(s): $($missingVars -join ', ')"
        exit 1
    }

    Write-Host "Starting local Postgres (docker compose up -d)..."
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Waiting for Postgres to become healthy..."
    $deadline = (Get-Date).AddSeconds(60)
    while ($true) {
        $status = $null
        try {
            $psOutput = docker compose ps db --format json
            if ($psOutput) {
                $status = $psOutput | ConvertFrom-Json
            }
        }
        catch {
            $status = $null
        }

        if ($status -and $status.Health -eq "healthy") {
            break
        }
        if ((Get-Date) -gt $deadline) {
            Write-Error "Postgres did not become healthy within 60 seconds."
            exit 1
        }
        Start-Sleep -Seconds 2
    }

    Write-Host "Applying roles (scripts/database/001_roles.sql)..."
    # Explicit on every run - see the header comment on why this can't rely on
    # docker-entrypoint-initdb.d alone. Placeholder substitution mirrors dev-init.sh.
    $rolesSql = Get-Content "scripts/database/001_roles.sql" -Raw
    $rolesSql = $rolesSql.Replace('${COMMITAHEAD_MIGRATOR_PASSWORD}', $envValues["COMMITAHEAD_MIGRATOR_PASSWORD"])
    $rolesSql = $rolesSql.Replace('${COMMITAHEAD_APP_PASSWORD}', $envValues["COMMITAHEAD_APP_PASSWORD"])
    $rolesSql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying EF Core migrations (tables - authoritative source)..."
    $env:COMMITAHEAD_MIGRATION_CONNECTION = "Host=localhost;Port=5433;Database=commitahead;Username=commitahead_migrator;Password=$($envValues['COMMITAHEAD_MIGRATOR_PASSWORD'])"
    dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying RLS (scripts/database/002_rls_users.sql - authoritative source for roles/RLS)..."
    Get-Content "scripts/database/002_rls_users.sql" -Raw | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying Phase 1 grants/RLS (scripts/database/003_rls_phase1.sql)..."
    Get-Content "scripts/database/003_rls_phase1.sql" -Raw | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Local DB ready: roles + migrations + RLS applied."
}
finally {
    if ($null -eq $originalMigrationConnection) {
        Remove-Item Env:COMMITAHEAD_MIGRATION_CONNECTION -ErrorAction SilentlyContinue
    }
    else {
        $env:COMMITAHEAD_MIGRATION_CONNECTION = $originalMigrationConnection
    }
    Pop-Location
}
