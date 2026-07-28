using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Best-effort Supabase revoke (ADR-0006): a failure here must never stop logout from completing
/// — the controller always clears session cookies regardless of this call's outcome. Only a safe,
/// content-free error is logged.
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to revoke the Supabase access token during logout.");
        }
    }
}
