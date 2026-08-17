# CommitAhead - idempotent local Supabase user bootstrap (ADR-0023).
#
# Closed login (ADR-0015) rejects every email unless it matches an existing, enabled row in the
# local Postgres `users` table, keyed by a real Supabase Auth user id. A fresh `supabase start`
# instance has no users at all, so there is nothing yet for that row to reference. This script:
#   1. Creates the Supabase Auth user via GoTrue's Admin API against the LOCAL Supabase instance
#      (never Supabase Cloud - see supabase/config.toml, this only ever talks to 127.0.0.1),
#      or finds the existing one if the email was already created by a previous run.
#   2. Upserts the matching row in the LOCAL Postgres `users` table (backend/docker-compose.yml's
#      own `db` service - the application's own database, entirely separate from the Supabase
#      CLI's own internal Postgres on port 54322).
#
# Safe to re-run: the Supabase Admin API create call is treated as "already exists" on failure
# (falls back to looking the user up by email instead of erroring), and the local `users` upsert
# uses the same ON CONFLICT pattern as bootstrap-production-user.ps1.
#
# Requires `supabase start` already running (see README.md "Local Supabase (Development)") and
# backend/docker-compose.yml's `db` service up (setup-local-db.ps1).
#
# NOTE: keep this file plain ASCII - see setup-local-db.ps1's header for why.

param(
    [Parameter(Mandatory = $true)]
    [string]$Email
)

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
$repoRootDir = Split-Path -Parent $backendDir
Push-Location $repoRootDir

try {
    Write-Host "Reading local Supabase status..."
    $statusJson = npx -y supabase@latest status -o json
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $status = $statusJson | ConvertFrom-Json

    $apiUrl = $status.API_URL
    $serviceRoleKey = $status.SERVICE_ROLE_KEY
    if ([string]::IsNullOrWhiteSpace($apiUrl) -or [string]::IsNullOrWhiteSpace($serviceRoleKey)) {
        Write-Error "Could not read API_URL/SERVICE_ROLE_KEY from 'supabase status' - is the local Supabase instance running (supabase start)?"
        exit 1
    }

    $normalizedEmail = $Email.Trim().ToLowerInvariant()
    $headers = @{
        "apikey"        = $serviceRoleKey
        "Authorization" = "Bearer $serviceRoleKey"
        "Content-Type"  = "application/json"
    }

    Write-Host "Creating (or finding) the local Supabase Auth user for $normalizedEmail..."
    $supabaseUserId = $null
    try {
        $body = @{ email = $normalizedEmail; email_confirm = $true } | ConvertTo-Json
        $created = Invoke-RestMethod -Method Post -Uri "$apiUrl/auth/v1/admin/users" -Headers $headers -Body $body
        $supabaseUserId = $created.id
    }
    catch {
        # Already exists from a previous run of this script - look it up instead of failing.
        $existingUsers = Invoke-RestMethod -Method Get -Uri "$apiUrl/auth/v1/admin/users" -Headers $headers
        $match = $existingUsers.users | Where-Object { $_.email -eq $normalizedEmail } | Select-Object -First 1
        if ($null -eq $match) {
            Write-Error "Failed to create the Supabase user and no existing user matches $normalizedEmail. Original error: $($_.Exception.Message)"
            exit 1
        }
        $supabaseUserId = $match.id
    }

    Write-Host "Supabase Auth user id: $supabaseUserId"

    # Basic SQL-literal escaping (doubling embedded single quotes) - values come from a local,
    # operator-controlled parameter and GoTrue's own response, not untrusted request input, but
    # this costs nothing.
    $escapedUserId = $supabaseUserId.Replace("'", "''")
    $escapedEmail = $normalizedEmail.Replace("'", "''")

    $upsertSql = @"
INSERT INTO users (id, supabase_user_id, email, is_enabled, created_at_utc)
VALUES (gen_random_uuid(), '$escapedUserId', '$escapedEmail', true, now())
ON CONFLICT (supabase_user_id) DO UPDATE SET email = EXCLUDED.email, is_enabled = true;
"@

    Write-Host "Seeding/updating the enabled local User row for $normalizedEmail..."
    Push-Location $backendDir
    try {
        $upsertSql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        Pop-Location
    }

    Write-Host "Bootstrap complete - $normalizedEmail can now sign in via magic link (check Mailpit at $($status.MAILPIT_URL))."
}
finally {
    Pop-Location
}
