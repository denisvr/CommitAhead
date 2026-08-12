# CommitAhead - manual restore of a backup made by backup-production-db.ps1 (ADR-0021).
#
# Copies the binary pg_dump custom-format file into the container with `docker compose cp` (a raw
# file copy, never a PowerShell text stream - see backup-production-db.ps1 for why that matters:
# accented/non-ASCII user content must round-trip byte-for-byte) and restores it with `pg_restore`
# run INSIDE the container, wrapped in `--single-transaction --exit-on-error` - an atomic
# all-or-nothing restore. Any error rolls the whole thing back instead of leaving the database
# half-restored (some tables recreated from the backup, others still from before). `--clean
# --if-exists` (inside that same transaction) drops existing objects before recreating them, so
# this works against either an empty database or one that already has the current schema.
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
# block `--clean`'s DROP statements, or read from a table mid-DROP/CREATE). Detects whether the app
# was actually running BEFORE this script touched anything, and restarts it afterward only if all
# three hold: the restore succeeded, the post-restore commitahead_app connection check succeeded,
# AND it was running before. If restore or verification fails, the app is deliberately left
# stopped and the script exits non-zero - restarting an app pointed at a database that failed to
# restore (or whose grants came back wrong) would be worse than leaving it down.
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
        throw "Backup file not found: $BackupFile"
    }

    $composeFile = Join-Path $repoRootDir "docker-compose.prod.yml"
    $composeArgs = @("-f", $composeFile)
    $envFile = ".env.production"
    if (Test-Path $envFile) {
        $composeArgs += @("--env-file", $envFile)
    }

    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmssfff")
    $containerTempPath = "/tmp/commitahead-restore-$timestamp.dump"

    # Detect BEFORE touching anything, so a stack where the app was never started (or was already
    # stopped for some other reason) stays that way afterward, rather than this script starting it
    # as a side effect.
    $wasRunning = $false
    try {
        $psJson = docker compose @composeArgs ps app --format json 2>$null
        if ($psJson) {
            $status = $psJson | ConvertFrom-Json
            $wasRunning = ($status.State -eq "running")
        }
    }
    catch {
        $wasRunning = $false
    }

    Write-Host "Stopping the app (currently running: $wasRunning) so it holds no connections during the restore..."
    docker compose @composeArgs stop app
    if ($LASTEXITCODE -ne 0) { throw "Failed to stop the app service (exit code $LASTEXITCODE)." }

    $restoreSucceeded = $false
    $verificationSucceeded = $false
    $failure = $null

    try {
        Write-Host "Copying $BackupFile into the db container..."
        docker compose @composeArgs cp $BackupFile "db:${containerTempPath}"
        if ($LASTEXITCODE -ne 0) { throw "Failed to copy the backup file into the db container (exit code $LASTEXITCODE)." }

        Write-Host "Restoring (pg_restore --single-transaction --exit-on-error --clean --if-exists, run inside the container)..."
        docker compose @composeArgs exec -T db pg_restore -U postgres -d commitahead --single-transaction --exit-on-error --clean --if-exists --no-comments $containerTempPath
        if ($LASTEXITCODE -ne 0) { throw "pg_restore failed (exit code $LASTEXITCODE) - the transaction was rolled back; leaving the app stopped." }
        $restoreSucceeded = $true

        Write-Host "Verifying the database still accepts connections as commitahead_app..."
        docker compose @composeArgs exec -T db psql -U commitahead_app -d commitahead -c "SELECT 1;" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Restore finished, but commitahead_app could not connect afterward (check ownership/grants) - leaving the app stopped." }
        $verificationSucceeded = $true
    }
    catch {
        $failure = $_
    }
    finally {
        # Always attempt to remove the temp dump from the container, whether the restore above
        # succeeded or not - but a cleanup failure here must never mask the real restore/
        # verification result captured in $failure above, so it's only ever a warning.
        docker compose @composeArgs exec -T db rm -f $containerTempPath 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not remove the temporary dump file ($containerTempPath) from the container (exit code $LASTEXITCODE)."
        }
    }

    if ($failure) {
        throw $failure
    }

    if ($wasRunning) {
        Write-Host "Restarting the app (it was running before the restore)..."
        docker compose @composeArgs up -d app
        if ($LASTEXITCODE -ne 0) { throw "Restore and verification succeeded, but restarting the app failed (exit code $LASTEXITCODE)." }
    }
    else {
        Write-Host "App was not running before the restore - leaving it stopped."
    }

    Write-Host "Restore complete."
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location
}
