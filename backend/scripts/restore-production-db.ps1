# CommitAhead - manual restore of a backup made by backup-production-db.ps1 (ADR-0021).
#
# The backup already contains DROP ... IF EXISTS statements before every CREATE (pg_dump
# --clean --if-exists), so this can target either an empty database or one that already has the
# current schema - both end up matching the backup's own contents.
#
# Usage: backend/scripts/restore-production-db.ps1 -BackupFile "backups/commitahead-<timestamp>.sql"
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

    Write-Host "Restoring $BackupFile into the production-like database..."
    Get-Content $BackupFile -Raw | docker compose @composeArgs exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Restore complete."
}
finally {
    Pop-Location
}
