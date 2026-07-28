using System.Net;
using System.Net.Http.Json;

namespace CommitAhead.Api.Tests.Auth;

public class LogoutEndpointTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;

    public LogoutEndpointTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SupabaseAuth.Reset();
    }

    [Fact]
    public async Task Post_WhenSupabaseRevokeFails_StillReturnsNoContent_AndClearsCookies()
    {
        _factory.SupabaseAuth.ExceptionToThrowOnRevoke = new HttpRequestException("Supabase is unreachable");
        var client = _factory.CreateClient();
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", $"{csrfCookie}; commitahead_access=some-access-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(setCookies, c => c.StartsWith("commitahead_access=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookies, c => c.StartsWith("commitahead_refresh=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookies, c => c.StartsWith("commitahead_session_started=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_WithoutAnAccessTokenCookie_StillReturnsNoContent_AndClearsCookies()
    {
        var client = _factory.CreateClient();
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        request.Headers.Add("Cookie", csrfCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("commitahead_session_started=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
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
