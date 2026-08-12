# CommitAhead - idempotent local-Docker user bootstrap (ADR-0021).
#
# A freshly-migrated production-like database has no User row at all, and closed login
# (ADR-0015) rejects every email unless it matches an existing, enabled User - so without this
# step, nobody can ever sign in to the local Docker stack. This inserts/updates exactly ONE row in
# the local PostgreSQL `users` table; it never creates a Supabase account, never enables public
# signup, and never hardcodes a specific person - the id/email are read from
# backend/.env.production (or -UserId/-Email parameters, which take priority).
#
# The Supabase Auth user with this exact id must already exist in the real Supabase project
# (see README.md "Setting Up the Real Supabase Project") - this script only ever touches the local
# PostgreSQL `users` table, matching the existing INSERT INTO users example there.
#
# Safe to re-run: upserts on the `users` table's own unique index (supabase_user_id), so running
# this again with the same id just refreshes the email and re-enables the row.
#
# NOTE: keep this file plain ASCII - see setup-local-db.ps1's header for why.

param(
    [string]$UserId,
    [string]$Email
)

$ErrorActionPreference = "Stop"

$backendDir = Split-Path -Parent $PSScriptRoot
$repoRootDir = Split-Path -Parent $backendDir
Push-Location $backendDir

try {
    $envFile = ".env.production"
    $envValues = @{}
    if (Test-Path $envFile) {
        Get-Content $envFile | ForEach-Object {
            if ($_ -match '^\s*([^#=]+)\s*=\s*(.*)\s*$') {
                $envValues[$matches[1].Trim()] = $matches[2].Trim()
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($UserId)) {
        $UserId = $envValues["INITIAL_USER_ID"]
    }
    if ([string]::IsNullOrWhiteSpace($Email)) {
        $Email = $envValues["INITIAL_USER_EMAIL"]
    }

    if ([string]::IsNullOrWhiteSpace($UserId) -or [string]::IsNullOrWhiteSpace($Email)) {
        Write-Error "No user id/email available - set INITIAL_USER_ID/INITIAL_USER_EMAIL in backend/.env.production, or pass -UserId/-Email."
        exit 1
    }

    if ($UserId -eq "change_me" -or $Email -eq "change_me") {
        Write-Error "INITIAL_USER_ID/INITIAL_USER_EMAIL still hold the placeholder 'change_me' - set your real Supabase user id/email in backend/.env.production first."
        exit 1
    }

    $parsedGuid = [Guid]::Empty
    if (-not [Guid]::TryParse($UserId, [ref]$parsedGuid)) {
        Write-Error "INITIAL_USER_ID ('$UserId') is not a valid GUID - it must be the Supabase Auth user's own UID."
        exit 1
    }

    # Same normalization LoginUseCase applies to every login attempt (User.Normalize) - the seeded
    # row must match it exactly, or closed login will never find this user.
    $normalizedEmail = $Email.Trim().ToLowerInvariant()

    # Basic SQL-literal escaping (doubling embedded single quotes) - the values come from a local,
    # operator-controlled file/parameter, not untrusted request input, but this costs nothing.
    $escapedUserId = $parsedGuid.ToString().Replace("'", "''")
    $escapedEmail = $normalizedEmail.Replace("'", "''")

    $composeFile = Join-Path $repoRootDir "docker-compose.prod.yml"
    $composeArgs = @("-f", $composeFile)
    if (Test-Path $envFile) {
        $composeArgs += @("--env-file", $envFile)
    }

    $upsertSql = @"
INSERT INTO users (id, supabase_user_id, email, is_enabled, created_at_utc)
VALUES ('$escapedUserId', '$escapedUserId', '$escapedEmail', true, now())
ON CONFLICT (supabase_user_id) DO UPDATE SET email = EXCLUDED.email, is_enabled = true;
"@

    # --env-file is required here even though this exec targets only "db" - without it, compose
    # still evaluates every ${VAR} in the file for every service while loading it and prints a
    # "variable not set" warning to stderr per undefined one; Windows PowerShell 5.1 sometimes
    # wraps those stderr lines in a terminating NativeCommandError even though the command itself
    # exits 0, making the script fail intermittently for no real reason.
    Write-Host "Seeding/updating the enabled User row for $normalizedEmail..."
    $upsertSql | docker compose @composeArgs exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d commitahead
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "User bootstrap complete - $normalizedEmail can now sign in."
}
finally {
    Pop-Location
}
