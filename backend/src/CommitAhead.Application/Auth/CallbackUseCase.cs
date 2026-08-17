using CommitAhead.Application.Identity;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.Auth;

public sealed class CallbackUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CallbackUseCase> _logger;

    public CallbackUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository, ILogger<CallbackUseCase> logger)
    {
        _authClient = authClient;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AuthResult> ExecuteAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
    {
        SupabaseTokenResult tokens;
        try
        {
            tokens = await _authClient.ExchangePkceCodeAsync(authCode, codeVerifier, cancellationToken);
        }
        // See LoginUseCase for why only genuine cancellation of OUR OWN cancellationToken should
        // propagate — an HttpClient provider timeout (or an unconfigured Supabase:Url, surfaced as
        // an InvalidOperationException from HttpClient itself) must fail this callback like any
        // other Supabase-call failure, not bubble up as an unhandled exception.
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Failed to exchange the Supabase PKCE code. Exception type: {ExceptionType}", ex.GetType().Name);
            return AuthResult.Denied();
        }

        var user = await _userRepository.GetBySupabaseUserIdAsync(tokens.SupabaseUserId, cancellationToken);
        if (user is null || !user.IsEnabled)
        {
            return AuthResult.Denied();
        }

        return AuthResult.Allowed(tokens);
    }
}
