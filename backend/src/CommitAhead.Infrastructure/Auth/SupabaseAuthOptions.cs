namespace CommitAhead.Infrastructure.Auth;

/// <summary>
/// Bound from the "Supabase" configuration section (user-secrets locally). Never logged; the
/// anon key is backend-only per ADR-0006 even though it is not itself ultra-sensitive.
/// </summary>
public sealed class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public required string Url { get; set; }

    public required string AnonKey { get; set; }
}
