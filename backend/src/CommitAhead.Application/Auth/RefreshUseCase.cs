using CommitAhead.Application.Identity;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Re-checks ADR-0015 on every refresh, not just at login — a user disabled after signing in
/// must lose access on their next refresh, not just their next fresh login.
/// </summary>
public sealed class RefreshUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;

    public RefreshUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository)
    {
        _authClient = authClient;
        _userRepository = userRepository;
    }

    public async Task<AuthResult> ExecuteAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokens = await _authClient.RefreshAsync(refreshToken, cancellationToken);

        var user = await _userRepository.GetBySupabaseUserIdAsync(tokens.SupabaseUserId, cancellationToken);
        if (user is null || !user.IsEnabled)
        {
            return AuthResult.Denied();
        }

        return AuthResult.Allowed(tokens);
    }
}
