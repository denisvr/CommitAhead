# CommitAhead - builds a self-contained EF Core migration bundle (ADR-0021, roadmap Phase 6 "Build
# reviewed EF migration bundle"): a single portable executable that applies pending migrations
# without needing the .NET SDK installed on the target machine - the reviewed artifact you'd run
# against a real deployment target once one is chosen, instead of requiring `dotnet ef` there too.
#
# setup-production-db.ps1 does NOT use this bundle - it already has the SDK on the dev machine and
# uses `dotnet ef database update` directly, same as setup-local-db.ps1. This script exists for the
# case that machine doesn't have the SDK (e.g. a minimal deployment host).
#
# Output: backend/artifacts/efbundle(.exe) - gitignored, rebuilt on demand, never committed.

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
Push-Location $backendDir

try {
    $outputDir = "artifacts"
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    Write-Host "Building self-contained EF Core migration bundle (linux-x64) into $outputDir\..."
    dotnet ef migrations bundle `
        --project src/CommitAhead.Infrastructure `
        --startup-project src/CommitAhead.Api `
        --self-contained `
        --runtime linux-x64 `
        --force `
        --output "$outputDir/efbundle"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Built $outputDir/efbundle. Run it against a target database with:"
    Write-Host "  ./$outputDir/efbundle --connection ""Host=<host>;Port=<port>;Database=commitahead;Username=commitahead_migrator;Password=<password>"""
}
finally {
    Pop-Location
}
