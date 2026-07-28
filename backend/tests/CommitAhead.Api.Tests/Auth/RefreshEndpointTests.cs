using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Security;
using CommitAhead.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Api.Tests.Auth;

public class RefreshEndpointTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;
    private readonly SessionStartToken _sessionStartToken;

    public RefreshEndpointTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Users.Clear();
        _sessionStartToken = factory.Services.GetRequiredService<SessionStartToken>();
    }

    [Fact]
    public async Task Post_WithFreshSessionAndValidRefreshToken_Succeeds()
    {
        // StubSupabaseAuthClient.RefreshAsync always reports SupabaseUserId "stub-sub" —
        // RefreshUseCase re-checks ADR-0015 against it on every refresh.
        _factory.Users.Add(new User(Guid.NewGuid(), "stub-sub", "owner@example.com", DateTime.UtcNow));
        var client = _factory.CreateClient();
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);
        var sessionStarted = _sessionStartToken.Protect(DateTimeOffset.UtcNow.AddHours(-1));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_session_started={sessionStarted}; commitahead_refresh=stub-refresh-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithSessionOlderThanSevenDays_ReturnsUnauthorized_AndClearsCookies()
    {
        var client = _factory.CreateClient();
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);
        var sessionStarted = _sessionStartToken.Protect(DateTimeOffset.UtcNow.AddDays(-8));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_session_started={sessionStarted}; commitahead_refresh=stub-refresh-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("commitahead_session_started=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_WithTamperedSessionStartedCookie_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_session_started=not-a-real-protected-value; commitahead_refresh=stub-refresh-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<(string Token, string Cookie)> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/auth/csrf");
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("commitahead_csrf=", StringComparison.Ordinal));
        var cookiePair = setCookie[..setCookie.IndexOf(';')];

        var body = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        return (body!.Token, cookiePair);
    }

    private sealed record CsrfResponse(string Token);
}
