using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Best-effort Supabase revoke (ADR-0006): a failure here must never stop logout from completing
/// — the controller always clears session cookies regardless of this call's outcome. Only a safe
/// event/error type is logged — never the access token, never the raw exception (message/stack
/// trace).
/// </summary>
public sealed class LogoutUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly ILogger<LogoutUseCase> _logger;

    public LogoutUseCase(ISupabaseAuthClient authClient, ILogger<LogoutUseCase> logger)
    {
        _authClient = authClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            await _authClient.RevokeAsync(accessToken, cancellationToken);
        }
        // See LoginUseCase for why only genuine cancellation of OUR OWN cancellationToken should
        // propagate — an HttpClient provider timeout is also an OperationCanceledException, but
        // must be treated like any other revoke failure, not rethrown.
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Failed to revoke the Supabase access token during logout. Exception type: {ExceptionType}", ex.GetType().Name);
        }
    }
}
