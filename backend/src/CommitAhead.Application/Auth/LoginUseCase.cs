namespace CommitAhead.Application.Auth;

/// <summary>
/// Initiates a Supabase magic link with a fresh PKCE pair (ADR-0006). Always calls Supabase and
/// never reveals whether the email matched an account — the controller returns the same generic
/// response regardless of outcome.
/// </summary>
public sealed class LoginUseCase
{
    private readonly ISupabaseAuthClient _authClient;

    public LoginUseCase(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async Task<string> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        var codeVerifier = PkceChallenge.GenerateCodeVerifier();
        var codeChallenge = PkceChallenge.ComputeCodeChallenge(codeVerifier);

        await _authClient.InitiateMagicLinkAsync(email, codeChallenge, cancellationToken);

        return codeVerifier;
    }
}
