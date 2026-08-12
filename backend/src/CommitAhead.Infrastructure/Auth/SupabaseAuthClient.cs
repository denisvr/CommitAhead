using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CommitAhead.Application.Auth;
using Microsoft.Extensions.Options;

namespace CommitAhead.Infrastructure.Auth;

/// <summary>
/// HttpClient-based ISupabaseAuthClient against GoTrue's REST API. Every call carries the
/// project's anon key via the "apikey" header, as GoTrue requires (ADR-0006).
/// </summary>
public sealed class SupabaseAuthClient : ISupabaseAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly string _callbackUrl;

    public SupabaseAuthClient(HttpClient httpClient, IOptions<AuthOptions> authOptions)
    {
        _httpClient = httpClient;
        _callbackUrl = authOptions.Value.CallbackUrl;
    }

    public async Task InitiateMagicLinkAsync(string email, string codeChallenge, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/otp")
        {
            Content = JsonContent.Create(new OtpRequest(email, false, codeChallenge, "s256", _callbackUrl)),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<SupabaseTokenResult> ExchangePkceCodeAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/token?grant_type=pkce")
        {
            Content = JsonContent.Create(new PkceTokenRequest(authCode, codeVerifier)),
        };

        return SendTokenRequestAsync(request, cancellationToken);
    }

    public Task<SupabaseTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/token?grant_type=refresh_token")
        {
            Content = JsonContent.Create(new RefreshTokenRequest(refreshToken)),
        };

        return SendTokenRequestAsync(request, cancellationToken);
    }

    public async Task RevokeAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/logout?scope=global")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<SupabaseTokenResult> SendTokenRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Supabase token response body was empty.");

        return new SupabaseTokenResult(
            body.AccessToken,
            body.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(body.ExpiresIn),
            body.User.Id);
    }

    private sealed record OtpRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("create_user")] bool CreateUser,
        [property: JsonPropertyName("code_challenge")] string CodeChallenge,
        [property: JsonPropertyName("code_challenge_method")] string CodeChallengeMethod,
        [property: JsonPropertyName("redirect_to")] string RedirectTo);

    private sealed record PkceTokenRequest(
        [property: JsonPropertyName("auth_code")] string AuthCode,
        [property: JsonPropertyName("code_verifier")] string CodeVerifier);

    private sealed record RefreshTokenRequest(
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("user")] TokenUser User);

    private sealed record TokenUser([property: JsonPropertyName("id")] string Id);
}
