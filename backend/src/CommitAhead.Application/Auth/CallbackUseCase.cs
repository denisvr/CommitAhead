using CommitAhead.Application.Identity;

namespace CommitAhead.Application.Auth;

public sealed class CallbackUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;

    public CallbackUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository)
    {
        _authClient = authClient;
        _userRepository = userRepository;
    }

    public async Task<AuthResult> ExecuteAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
    {
        var tokens = await _authClient.ExchangePkceCodeAsync(authCode, codeVerifier, cancellationToken);

        var user = await _userRepository.GetBySupabaseUserIdAsync(tokens.SupabaseUserId, cancellationToken);
        if (user is null || !user.IsEnabled)
        {
            return AuthResult.Denied();
        }

        return AuthResult.Allowed(tokens);
    }
}
