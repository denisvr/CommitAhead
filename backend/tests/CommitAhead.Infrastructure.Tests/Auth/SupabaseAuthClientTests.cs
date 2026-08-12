using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CommitAhead.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CommitAhead.Infrastructure.Tests.Auth;

public class SupabaseAuthClientTests
{
    private static readonly Uri BaseAddress = new("https://project.supabase.co");

    [Fact]
    public async Task InitiateMagicLinkAsync_PostsOtpRequest_WithPkceChallenge()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await client.InitiateMagicLinkAsync("owner@example.com", "challenge-value", CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/auth/v1/otp", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"email\":\"owner@example.com\"", handler.LastRequestBody);
        Assert.Contains("\"create_user\":false", handler.LastRequestBody);
        Assert.Contains("\"code_challenge\":\"challenge-value\"", handler.LastRequestBody);
        Assert.Contains("\"code_challenge_method\":\"s256\"", handler.LastRequestBody);
        Assert.Contains("\"redirect_to\":\"https://configured.example/auth/callback\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task ExchangePkceCodeAsync_PostsPkceGrant_AndParsesTheTokenResponse()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            """
            {"access_token":"at-1","refresh_token":"rt-1","expires_in":3600,"user":{"id":"sub-1"}}
            """));
        var client = CreateClient(handler);

        var result = await client.ExchangePkceCodeAsync("auth-code", "code-verifier", CancellationToken.None);

        Assert.Equal("/auth/v1/token", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("grant_type=pkce", handler.LastRequest.RequestUri!.Query.TrimStart('?'));
        Assert.Contains("\"auth_code\":\"auth-code\"", handler.LastRequestBody);
        Assert.Contains("\"code_verifier\":\"code-verifier\"", handler.LastRequestBody);
        Assert.Equal("at-1", result.AccessToken);
        Assert.Equal("rt-1", result.RefreshToken);
        Assert.Equal("sub-1", result.SupabaseUserId);
        Assert.True(result.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(50));
    }

    [Fact]
    public async Task RefreshAsync_PostsRefreshGrant()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            """
            {"access_token":"at-2","refresh_token":"rt-2","expires_in":3600,"user":{"id":"sub-1"}}
            """));
        var client = CreateClient(handler);

        var result = await client.RefreshAsync("rt-1", CancellationToken.None);

        Assert.Equal("grant_type=refresh_token", handler.LastRequest!.RequestUri!.Query.TrimStart('?'));
        Assert.Contains("\"refresh_token\":\"rt-1\"", handler.LastRequestBody);
        Assert.Equal("at-2", result.AccessToken);
    }

    [Fact]
    public async Task RevokeAsync_PostsLogout_WithBearerAccessToken()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.RevokeAsync("at-1", CancellationToken.None);

        Assert.Equal("/auth/v1/logout", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("at-1", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ExchangePkceCodeAsync_WhenSupabaseReturnsAnError_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.ExchangePkceCodeAsync("bad-code", "code-verifier", CancellationToken.None));
    }

    private static SupabaseAuthClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseAddress };
        httpClient.DefaultRequestHeaders.Add("apikey", "test-anon-key");
        var authOptions = Options.Create(new AuthOptions { CallbackUrl = "https://configured.example/auth/callback" });
        return new SupabaseAuthClient(httpClient, authOptions);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}
