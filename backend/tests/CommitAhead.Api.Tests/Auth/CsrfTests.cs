using System.Net;
using System.Net.Http.Json;

namespace CommitAhead.Api.Tests.Auth;

public class CsrfTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;

    public CsrfTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_HasNoSessionYet_IsExemptFromCsrf()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "owner@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCsrfToken_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCsrfToken_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidCsrfToken_PassesCsrfAndReachesTheController()
    {
        var client = _factory.CreateClient();

        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", csrfCookie);

        var response = await client.SendAsync(request);

        // CSRF passed; the controller itself rejects for lack of a session — proves the request
        // reached business logic instead of being blocked at 400 by CsrfMiddleware.
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
