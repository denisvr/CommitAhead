using CommitAhead.Application.Identity;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Always returns a PKCE code verifier so the response shape is identical whether or not the
/// email is provisioned (ADR-0015: closed login, no enumeration). Supabase is only called — the
/// only externally-visible/timed side effect — when the email matches an existing, enabled User;
/// the controller returns the same generic response regardless of outcome.
/// </summary>
public sealed class LoginUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;

    public LoginUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository)
    {
        _authClient = authClient;
        _userRepository = userRepository;
    }

    public async Task<string> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.Normalize(email);
        var codeVerifier = PkceChallenge.GenerateCodeVerifier();
        var codeChallenge = PkceChallenge.ComputeCodeChallenge(codeVerifier);

        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is not null && user.IsEnabled)
        {
            await _authClient.InitiateMagicLinkAsync(normalizedEmail, codeChallenge, cancellationToken);
        }

        return codeVerifier;
    }
}
