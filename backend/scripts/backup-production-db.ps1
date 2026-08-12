# CommitAhead - manual backup of the local production-like PostgreSQL (ADR-0021).
#
# This is deliberately simple and manual, not an automated/scheduled backup system - encrypted,
# automated backups with a real retention policy are still an open decision (docs/tbd.md) for
# whenever a hosting platform is chosen. This is the "simple usable command" for the local Docker
# stack in the meantime, since that stack is meant to be used extensively before then.
#
# Plain-text SQL dump (--format=plain), not pg_dump's binary custom format - piping a binary
# stream through PowerShell's pipeline/redirection risks silent corruption (encoding/newline
# translation); plain SQL text has no such risk and restores with a plain `psql` invocation.
# Written as ASCII on purpose (see restore-production-db.ps1) - this schema's own DDL/seed data is
# ASCII-only; a non-ASCII value (e.g. an accented name) would be replaced with '?' in the dump, an
# accepted trade-off for a simple local-only command.
#
# Usage: backend/scripts/backup-production-db.ps1
# Output: backend/backups/commitahead-<UTC timestamp>.sql (gitignored)
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
    $outputFile = Join-Path $backupsDir "commitahead-$timestamp.sql"

    # --env-file (when present) avoids compose's own "variable not set" stderr warnings while
    # loading the file - see bootstrap-production-user.ps1 for why those can otherwise turn into a
    # spurious terminating error on Windows PowerShell 5.1.
    Write-Host "Dumping the production-like database to $outputFile..."
    docker compose @composeArgs exec -T db pg_dump -U postgres -d commitahead --format=plain --no-owner --no-privileges --clean --if-exists |
        Out-File -FilePath $outputFile -Encoding ascii
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Backup written to $outputFile. Restore with:"
    Write-Host "  backend/scripts/restore-production-db.ps1 -BackupFile `"$outputFile`""
}
finally {
    Pop-Location
}
