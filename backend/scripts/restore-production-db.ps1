# CommitAhead - manual restore of a backup made by backup-production-db.ps1 (ADR-0021).
#
# Copies the binary pg_dump custom-format file into the container with `docker compose cp` (a raw
# file copy, never a PowerShell text stream - see backup-production-db.ps1 for why that matters:
# accented/non-ASCII user content must round-trip byte-for-byte) and restores it with `pg_restore`
# run INSIDE the container. `--clean --if-exists` drops existing objects (matched by name) before
# recreating them, so this works against either an empty database or one that already has the
# current schema.
#
# Ownership and grants are restored exactly as dumped (backup-production-db.ps1 deliberately omits
# --no-owner/--no-privileges) - pg_restore, running as the postgres superuser, reassigns table
# ownership back to commitahead_migrator and grants back to commitahead_app from the dump's own
# ALTER TABLE OWNER TO / GRANT statements. This assumes those roles already exist in the target
# database (i.e. setup-production-db.ps1 has run at least once) - restoring into a database that
# has never had roles created will emit (non-fatal) errors for the ownership/grant statements only.
# RLS policies are ordinary table metadata to pg_dump/pg_restore, so they come back automatically
# with no separate step to reapply 002_rls_users.sql/003-007 afterward.
#
# Stops the app for the duration of the restore (a live connection could otherwise hold locks that
# block `--clean`'s DROP statements, or read from a table mid-DROP/CREATE) and restarts it
# afterward - `docker compose up -d app` rather than `start`, so it also works if the app container
# was never created in the first place.
#
# Usage: backend/scripts/restore-production-db.ps1 -BackupFile "backups/commitahead-<timestamp>.dump"
#
# NOTE: keep this file plain ASCII - see setup-local-db.ps1's header for why.

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
$repoRootDir = Split-Path -Parent $backendDir
Push-Location $backendDir

try {
    if (-not (Test-Path $BackupFile)) {
        Write-Error "Backup file not found: $BackupFile"
        exit 1
    }

    $composeFile = Join-Path $repoRootDir "docker-compose.prod.yml"
    $composeArgs = @("-f", $composeFile)
    $envFile = ".env.production"
    if (Test-Path $envFile) {
        $composeArgs += @("--env-file", $envFile)
    }

    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmssfff")
    $containerTempPath = "/tmp/commitahead-restore-$timestamp.dump"

    Write-Host "Stopping the app so it holds no connections during the restore..."
    docker compose @composeArgs stop app
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    try {
        Write-Host "Copying $BackupFile into the db container..."
        docker compose @composeArgs cp $BackupFile "db:${containerTempPath}"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        Write-Host "Restoring (pg_restore --clean --if-exists, run inside the container)..."
        docker compose @composeArgs exec -T db pg_restore -U postgres -d commitahead --clean --if-exists --no-comments $containerTempPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        docker compose @composeArgs exec -T db rm -f $containerTempPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        Write-Host "Restarting the app..."
        docker compose @composeArgs up -d app
    }

    Write-Host "Verifying the database still accepts connections as commitahead_app..."
    docker compose @composeArgs exec -T db psql -U commitahead_app -d commitahead -c "SELECT 1;" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore finished, but commitahead_app could not connect afterward - check ownership/grants."
        exit 1
    }

    Write-Host "Restore complete."
}
finally {
    Pop-Location
}
