namespace CommitAhead.Api.Security;

/// <summary>
/// Bound from the "E2E" configuration section. These values have no production meaning — they
/// exist only so the E2E Docker stack (docs/testing/strategy.md §7.3) can mint a locally-signed
/// session for its one seeded user without ever touching Supabase. E2EConfigurationGuard enforces
/// that this section is populated if and only if ASPNETCORE_ENVIRONMENT=E2E; nothing here is ever
/// derived from, or reused as, a real secret.
/// </summary>
public sealed class E2EOptions
{
    public const string SectionName = "E2E";

    public string? SigningKey { get; set; }

    public string? Issuer { get; set; }

    public string? SupabaseUserId { get; set; }
}
