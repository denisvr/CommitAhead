namespace CommitAhead.Application.Auth;

public sealed class LogoutUseCase
{
    private readonly ISupabaseAuthClient _authClient;

    public LogoutUseCase(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public Task ExecuteAsync(string accessToken, CancellationToken cancellationToken)
    {
        return _authClient.RevokeAsync(accessToken, cancellationToken);
    }
}
