namespace CommitAhead.Infrastructure.Auth;

/// <summary>
/// Bound from the "Auth" configuration section (appsettings/user-secrets/environment variables) —
/// trusted, protected application configuration only. Never derived from a request's Origin,
/// Referer, or any other caller-supplied value, so a caller can never redirect the Supabase
/// magic-link callback to an attacker-controlled URL. Differs per environment: local `dotnet run`
/// uses http://localhost:5120/auth/callback (appsettings.Development.json), the local Docker stack
/// uses http://localhost:8080/auth/callback (docker-compose.prod.yml's Auth__CallbackUrl) — both
/// must be present in the Supabase project's own redirect allow-list (see README.md).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public required string CallbackUrl { get; set; }
}
