# CommitAhead — reproducible local dev database bootstrap: roles -> migrations -> RLS.
#
# EF Core migrations are the authoritative source for tables; this script and the versioned SQL
# under scripts/database/ are the authoritative source for roles and RLS (see
# docs/architecture/persistence.md "Migration Strategy"). Never touches the real Supabase
# Postgres — this only ever targets the local Docker Postgres in docker-compose.yml.
#
# Safe to re-run: docker compose up is idempotent, EF migrations only apply what's pending, and
# 002_rls_users.sql drops+recreates its policy instead of failing on a second run.

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
Push-Location $backendDir

try {
    if (-not (Test-Path ".env")) {
        Write-Error ".env not found in backend/ — copy .env.example to .env and set real passwords first."
        exit 1
    }

    Write-Host "Starting local Postgres (docker compose up -d)..."
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Waiting for Postgres to become healthy..."
    $deadline = (Get-Date).AddSeconds(60)
    while ($true) {
        $status = docker compose ps db --format json | ConvertFrom-Json
        if ($status.Health -eq "healthy") { break }
        if ((Get-Date) -gt $deadline) {
            Write-Error "Postgres did not become healthy within 60 seconds."
            exit 1
        }
        Start-Sleep -Seconds 2
    }

    $envValues = @{}
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)\s*=\s*(.*)\s*$') {
            $envValues[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    Write-Host "Applying EF Core migrations (tables — authoritative source)..."
    $env:COMMITAHEAD_MIGRATION_CONNECTION = "Host=localhost;Port=5433;Database=commitahead;Username=commitahead_migrator;Password=$($envValues['COMMITAHEAD_MIGRATOR_PASSWORD'])"
    dotnet ef database update --project src/CommitAhead.Infrastructure --startup-project src/CommitAhead.Api
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Applying RLS (scripts/database/002_rls_users.sql — authoritative source for roles/RLS)..."
    Get-Content "scripts/database/002_rls_users.sql" -Raw | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Local DB ready: roles + migrations + RLS applied."
}
finally {
    Pop-Location
}
