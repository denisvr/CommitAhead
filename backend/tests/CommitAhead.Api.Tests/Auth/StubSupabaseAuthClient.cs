using CommitAhead.Application.Auth;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Replaces the real SupabaseAuthClient (which makes real HTTP calls) so API tests can exercise
/// routes like /auth/login without a network call — zero real calls, per testing/strategy.md.
/// </summary>
public sealed class StubSupabaseAuthClient : ISupabaseAuthClient
{
    public Task InitiateMagicLinkAsync(string email, string codeChallenge, CancellationToken cancellationToken)
    {
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
        return Task.CompletedTask;
    }
}
