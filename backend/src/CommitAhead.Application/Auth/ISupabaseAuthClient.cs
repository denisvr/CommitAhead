namespace CommitAhead.Application.Auth;

/// <summary>
/// Backend-mediated boundary to Supabase Auth (ADR-0006). The only place real calls to Supabase
/// Auth occur; never called from the frontend or domain layer. GoTrue's /otp endpoint has no
/// redirect override — the magic-link destination is the Supabase project's configured Site
/// URL / redirect allow-list, not a per-call parameter.
/// </summary>
public interface ISupabaseAuthClient
{
    Task InitiateMagicLinkAsync(string email, string codeChallenge, CancellationToken cancellationToken);

    Task<SupabaseTokenResult> ExchangePkceCodeAsync(string authCode, string codeVerifier, CancellationToken cancellationToken);

    Task<SupabaseTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed record SupabaseTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string SupabaseUserId);
