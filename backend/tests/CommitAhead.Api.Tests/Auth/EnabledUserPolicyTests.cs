using System.Net;
using System.Net.Http.Json;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Regression coverage for the corrected ADR-0015 enforcement: it must apply only to protected
/// resources (via the authorization fallback policy), never to the [AllowAnonymous] auth
/// endpoints — an authenticated-but-unknown/disabled-user access token must not block login,
/// refresh, or logout the way the old global EnabledUserMiddleware did.
/// </summary>
public class EnabledUserPolicyTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;

    public EnabledUserPolicyTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Users.Clear();
    }

    [Fact]
    public async Task Refresh_WithUnknownUserAccessTokenCookie_IsNotBlockedWithForbidden()
    {
        var client = _factory.CreateClient();
        var accessToken = JwtTestTokenFactory.CreateAccessToken("sub-unknown");
        // Fetch CSRF under the same authenticated-but-unknown-user cookie state as the follow-up
        // request — a real browser sends the access-token cookie on every same-origin request,
        // including this one, so the antiforgery token must be generated under that same identity.
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client, accessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_access={accessToken}");

        var response = await client.SendAsync(request);

        // No session-started/refresh-token cookies exist, so the controller itself returns 401.
        // The regression this guards against would have produced 403 instead, from the old
        // global middleware rejecting the request before it ever reached RefreshController.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithUnknownUserAccessTokenCookie_StillSucceedsAndClearsCookies()
    {
        var client = _factory.CreateClient();
        var accessToken = JwtTestTokenFactory.CreateAccessToken("sub-unknown-or-disabled");
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client, accessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_access={accessToken}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("commitahead_access=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(string Token, string Cookie)> GetCsrfTokenAsync(HttpClient client, string? accessTokenCookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");
        if (accessTokenCookie is not null)
        {
            request.Headers.Add("Cookie", $"commitahead_access={accessTokenCookie}");
        }

        var response = await client.SendAsync(request);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("commitahead_csrf=", StringComparison.Ordinal));
        var cookiePair = setCookie[..setCookie.IndexOf(';')];

        var body = await response.Content.ReadFromJsonAsync<CsrfResponse>();
        return (body!.Token, cookiePair);
    }

    private sealed record CsrfResponse(string Token);
}
