namespace CommitAhead.Application.Auth;

/// <summary>
/// Outcome of a token exchange (callback or refresh). Denied when the Supabase identity has no
/// matching enabled application User (ADR-0015) — an unrecognised identity, not a bad request.
/// </summary>
public sealed class AuthResult
{
    public bool IsAllowed { get; }
    public SupabaseTokenResult? Tokens { get; }

    private AuthResult(bool isAllowed, SupabaseTokenResult? tokens)
    {
        IsAllowed = isAllowed;
        Tokens = tokens;
    }

    public static AuthResult Allowed(SupabaseTokenResult tokens) => new(true, tokens);

    public static AuthResult Denied() => new(false, null);
}
