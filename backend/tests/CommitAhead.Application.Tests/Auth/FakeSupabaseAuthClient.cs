using CommitAhead.Application.Auth;

namespace CommitAhead.Application.Tests.Auth;

public sealed class FakeSupabaseAuthClient : ISupabaseAuthClient
{
    public string? LastEmail { get; private set; }
    public string? LastCodeChallenge { get; private set; }
    public string? LastRevokedAccessToken { get; private set; }
    public SupabaseTokenResult? TokenToReturn { get; set; }

    public Task InitiateMagicLinkAsync(string email, string codeChallenge, CancellationToken cancellationToken)
    {
        LastEmail = email;
        LastCodeChallenge = codeChallenge;
        return Task.CompletedTask;
    }

    public Task<SupabaseTokenResult> ExchangePkceCodeAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(TokenToReturn ?? throw new InvalidOperationException($"{nameof(TokenToReturn)} was not set."));
    }

    public Task<SupabaseTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(TokenToReturn ?? throw new InvalidOperationException($"{nameof(TokenToReturn)} was not set."));
    }

    public Task RevokeAsync(string accessToken, CancellationToken cancellationToken)
    {
        LastRevokedAccessToken = accessToken;
        return Task.CompletedTask;
    }
}
