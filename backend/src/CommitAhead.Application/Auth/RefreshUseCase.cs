using CommitAhead.Application.Identity;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Re-checks ADR-0015 on every refresh, not just at login — a user disabled after signing in
/// must lose access on their next refresh, not just their next fresh login.
/// </summary>
public sealed class RefreshUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RefreshUseCase> _logger;

    public RefreshUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository, ILogger<RefreshUseCase> logger)
    {
        _authClient = authClient;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AuthResult> ExecuteAsync(string refreshToken, CancellationToken cancellationToken)
    {
        SupabaseTokenResult tokens;
        try
        {
            tokens = await _authClient.RefreshAsync(refreshToken, cancellationToken);
        }
        // See LoginUseCase for why only genuine cancellation of OUR OWN cancellationToken should
        // propagate — an HttpClient provider timeout (or an unconfigured Supabase:Url, surfaced as
        // an InvalidOperationException from HttpClient itself) must fail this refresh attempt like
        // any other Supabase-call failure, not bubble up as an unhandled exception.
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Failed to refresh the Supabase session. Exception type: {ExceptionType}", ex.GetType().Name);
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
