using CommitAhead.Application.Auth;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Replaces the real SupabaseAuthClient (which makes real HTTP calls) so API tests can exercise
/// routes like /auth/login without a network call — zero real calls, per testing/strategy.md.
/// </summary>
public sealed class StubSupabaseAuthClient : ISupabaseAuthClient
{
    public bool MagicLinkInitiated { get; private set; }
    public string? LastMagicLinkEmail { get; private set; }
    public Exception? ExceptionToThrowOnInitiateMagicLink { get; set; }
    public Exception? ExceptionToThrowOnRevoke { get; set; }

    public void Reset()
    {
        MagicLinkInitiated = false;
        LastMagicLinkEmail = null;
        ExceptionToThrowOnInitiateMagicLink = null;
        ExceptionToThrowOnRevoke = null;
    }

    public Task InitiateMagicLinkAsync(string email, string codeChallenge, CancellationToken cancellationToken)
    {
        MagicLinkInitiated = true;
        LastMagicLinkEmail = email;

        if (ExceptionToThrowOnInitiateMagicLink is not null)
        {
            throw ExceptionToThrowOnInitiateMagicLink;
        }

        return Task.CompletedTask;
    }

    public Task<SupabaseTokenResult> ExchangePkceCodeAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SupabaseTokenResult("stub-access", "stub-refresh", DateTimeOffset.UtcNow.AddMinutes(15), "stub-sub"));
    }

    public Task<SupabaseTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SupabaseTokenResult("stub-access", "stub-refresh", DateTimeOffset.UtcNow.AddMinutes(15), "stub-sub"));
    }

    public Task RevokeAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (ExceptionToThrowOnRevoke is not null)
        {
            throw ExceptionToThrowOnRevoke;
        }

        return Task.CompletedTask;
    }
}
