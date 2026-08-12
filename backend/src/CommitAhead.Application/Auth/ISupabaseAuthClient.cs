namespace CommitAhead.Application.Auth;

/// <summary>
/// Backend-mediated boundary to Supabase Auth (ADR-0006). The only place real calls to Supabase
/// Auth occur; never called from the frontend or domain layer. The real implementation
/// (Infrastructure: SupabaseAuthClient) sends a redirect_to on the magic-link request as a
/// percent-encoded query parameter on the /otp call (GoTrue reads it only from there, not the JSON
/// body), sourced only from trusted backend configuration (AuthOptions.CallbackUrl) — never from a
/// request's Origin/Referer or any other caller-supplied value. That URL must also be present in
/// the Supabase project's own redirect allow-list (Authentication → URL Configuration), or Supabase
/// rejects it and falls back to the Site URL.
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
