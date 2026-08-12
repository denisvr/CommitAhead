# CommitAhead - reproducible production-like local database bootstrap (ADR-0021): roles ->
# migrations -> RLS(users) -> RLS(Phase 1) -> RLS(Phase 2) -> RLS(Phase 3) -> RLS(Phase 4).
#
# Mirrors setup-local-db.ps1 exactly, targeting docker-compose.prod.yml's db service instead of
# docker-compose.yml's - a distinct port (5434) and volume, so both stacks can exist side by side.
# Still never touches the real Supabase Postgres (see README.md "Setting Up the Real Supabase
# Project" for that, separate and manual).
#
# Safe to re-run for the same reasons as setup-local-db.ps1: 001_roles.sql is idempotent, EF
# migrations only apply what's pending, and every RLS script drops+recreates its own policies.
#
# NOTE: keep this file plain ASCII - see setup-local-db.ps1's header for why.

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
$repoRootDir = Split-Path -Parent $backendDir
Push-Location $backendDir

$originalMigrationConnection = $env:COMMITAHEAD_MIGRATION_CONNECTION

try {
    $envFile = ".env.production"
    if (-not (Test-Path $envFile)) {
        Write-Error "backend/.env.production not found - copy .env.production.example to .env.production and set real values first."
        exit 1
    }

    $envValues = @{}
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)\s*=\s*(.*)\s*$') {
            $envValues[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    $requiredVars = @("POSTGRES_PASSWORD", "COMMITAHEAD_MIGRATOR_PASSWORD", "COMMITAHEAD_APP_PASSWORD")
    $missingVars = $requiredVars | Where-Object { [string]::IsNullOrWhiteSpace($envValues[$_]) }
    if ($missingVars.Count -gt 0) {
        Write-Error "backend/.env.production is missing required value(s): $($missingVars -join ', ')"
        exit 1
    }

    $composeFile = Join-Path $repoRootDir "docker-compose.prod.yml"

    Write-Host "Starting the production-like Postgres (docker compose up -d db)..."
    docker compose -f $composeFile --env-file $envFile up -d db
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Waiting for Postgres to become healthy..."
    $deadline = (Get-Date).AddSeconds(60)
    while ($true) {
        $status = $null
        try {
            $psOutput = docker compose -f $composeFile ps db --format json
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
    $rolesSql = Get-Content "scripts/database/001_roles.sql" -Raw
    $rolesSql = $rolesSql.Replace('${COMMITAHEAD_MIGRATOR_PASSWORD}', $envValues["COMMITAHEAD_MIGRATOR_PASSWORD"])
    $rolesSql = $rolesSql.Replace('${COMMITAHEAD_APP_PASSWORD}', $envValues["COMMITAHEAD_APP_PASSWORD"])
    $rolesSql | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying EF Core migrations (tables - authoritative source)..."
    $env:COMMITAHEAD_MIGRATION_CONNECTION = "Host=localhost;Port=5434;Database=commitahead;Username=commitahead_migrator;Password=$($envValues['COMMITAHEAD_MIGRATOR_PASSWORD'])"
    dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying RLS (scripts/database/002_rls_users.sql - authoritative source for roles/RLS)..."
    Get-Content "scripts/database/002_rls_users.sql" -Raw | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying Phase 1 grants/RLS (scripts/database/003_rls_phase1.sql)..."
    Get-Content "scripts/database/003_rls_phase1.sql" -Raw | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying Phase 2 grants/RLS (scripts/database/004_rls_phase2.sql)..."
    Get-Content "scripts/database/004_rls_phase2.sql" -Raw | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying Phase 3 grants/RLS (scripts/database/005_rls_phase3.sql)..."
    Get-Content "scripts/database/005_rls_phase3.sql" -Raw | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying Phase 4 grants/RLS (scripts/database/007_rls_phase4.sql)..."
    Get-Content "scripts/database/007_rls_phase4.sql" -Raw | docker compose -f $composeFile exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Production-like DB ready: roles + migrations + RLS applied. Now run:"
    Write-Host "  docker compose -f docker-compose.prod.yml --env-file backend/.env.production up -d --build"
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
