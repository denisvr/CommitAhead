# CommitAhead - manual backup of the local production-like PostgreSQL (ADR-0021).
#
# This is deliberately simple and manual, not an automated/scheduled backup system - encrypted,
# automated backups with a real retention policy are still an open decision (docs/tbd.md) for
# whenever a hosting platform is chosen. This is the "simple usable command" for the local Docker
# stack in the meantime, since that stack is meant to be used extensively before then.
#
# pg_dump's binary custom format (--format=custom), produced entirely INSIDE the container and
# copied out with `docker compose cp` - a raw binary file copy, not a byte stream passed through
# PowerShell's pipeline/redirection. An earlier version of this script piped a text-mode dump
# through PowerShell text encoding (Out-File -Encoding ascii); that is lossy for anything outside
# ASCII (accented characters in user-entered profile text, e.g. Portuguese "experiencia") and was
# never actually lossless. This version never touches the dump's bytes in PowerShell at all.
#
# Deliberately does NOT pass --no-owner/--no-privileges: this backs up and restores against the
# SAME stack's own roles (commitahead_migrator/commitahead_app), so the dump's recorded ownership
# and grants are exactly what a correct restore needs - restore-production-db.ps1 relies on this to
# put table ownership back on commitahead_migrator (required for future EF migrations to work) and
# grants back on commitahead_app (required for the running app to work), not just the data.
#
# Usage: backend/scripts/backup-production-db.ps1
# Output: backend/backups/commitahead-<UTC timestamp>.dump (gitignored, pg_dump custom format)
#
# NOTE: keep this file plain ASCII - see setup-local-db.ps1's header for why.

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
$repoRootDir = Split-Path -Parent $backendDir
Push-Location $backendDir

try {
    $composeFile = Join-Path $repoRootDir "docker-compose.prod.yml"
    $composeArgs = @("-f", $composeFile)
    $envFile = ".env.production"
    if (Test-Path $envFile) {
        $composeArgs += @("--env-file", $envFile)
    }

    $backupsDir = "backups"
    New-Item -ItemType Directory -Force -Path $backupsDir | Out-Null

    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
    $outputFile = Join-Path $backupsDir "commitahead-$timestamp.dump"
    $containerTempPath = "/tmp/commitahead-backup-$timestamp.dump"

    Write-Host "Dumping the production-like database (inside the container)..."
    docker compose @composeArgs exec -T db pg_dump -U postgres -d commitahead --format=custom -f $containerTempPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Copying the dump out as a raw binary file to $outputFile..."
    docker compose @composeArgs cp "db:${containerTempPath}" $outputFile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    docker compose @composeArgs exec -T db rm -f $containerTempPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Backup written to $outputFile. Restore with:"
    Write-Host "  backend/scripts/restore-production-db.ps1 -BackupFile `"$outputFile`""
}
finally {
    Pop-Location
}
