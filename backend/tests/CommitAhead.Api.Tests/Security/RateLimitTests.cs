using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Security;
using CommitAhead.Api.Tests.TestInfrastructure;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// Enforcement tests for the two rate limits that protect authenticated work: the tight CV-export
/// policy and the global state-changing limiter.
///
/// Both run against the real MVC pipeline, and both rely on the limiters being partitioned by the
/// authenticated subject — each test provisions its own user, so it spends only its own budget and
/// cannot make a sibling test flake. Neither test needs fixture data: a 404 consumes a permit
/// exactly like a 200, which is the point (a limiter that only counted successful requests would be
/// useless against enumeration).
/// </summary>
[Collection(PostgresApiCollection.Name)]
public class RateLimitTests
{
    private readonly PostgresApiTestFactory _factory;

    public RateLimitTests(PostgresApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Export_BeyondItsWindowAllowance_IsRejectedWith429AndRetryAfter()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var url = $"/api/cv-presentations/{Guid.NewGuid()}/export";

        for (var attempt = 1; attempt <= TransportLimits.ExportsPerWindow; attempt++)
        {
            var allowed = await client.SendGetAsync(url, accessCookie);
            Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
        }

        var rejected = await client.SendGetAsync(url, accessCookie);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"), "A 429 must tell the caller when to retry.");
    }

    [Fact]
    public async Task Export_ForADifferentCaller_IsNotAffectedByAnExhaustedCaller()
    {
        var (exhaustedClient, exhaustedCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var exhaustedUrl = $"/api/cv-presentations/{Guid.NewGuid()}/export";

        for (var attempt = 0; attempt <= TransportLimits.ExportsPerWindow; attempt++)
        {
            await exhaustedClient.SendGetAsync(exhaustedUrl, exhaustedCookie);
        }

        var (otherClient, otherCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var response = await otherClient.SendGetAsync($"/api/cv-presentations/{Guid.NewGuid()}/export", otherCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StateChangingRequests_BeyondTheGlobalAllowance_AreRejectedWith429()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        // One CSRF token reused for every write. SendMutatingAsync fetches a fresh one per call,
        // which would double the request count for no added coverage here — CsrfTests already owns
        // the antiforgery contract.
        var (csrfToken, csrfCookie) = await GetCsrfAsync(client, accessCookie);

        for (var attempt = 1; attempt <= TransportLimits.WritesPerMinute; attempt++)
        {
            var allowed = await SendExperienceReplaceAsync(client, accessCookie, csrfToken, csrfCookie);
            Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
        }

        var rejected = await SendExperienceReplaceAsync(client, accessCookie, csrfToken, csrfCookie);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task SafeRequests_AreNotCountedAgainstTheStateChangingAllowance()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        for (var attempt = 0; attempt <= TransportLimits.WritesPerMinute; attempt++)
        {
            var response = await client.SendGetAsync("/api/professional-profile", accessCookie);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> SendExperienceReplaceAsync(
        HttpClient client,
        string accessCookie,
        string csrfToken,
        string csrfCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/professional-profile/experience")
        {
            Content = JsonContent.Create(Array.Empty<object>(), options: PostgresApiTestHelpers.JsonOptions),
        };
        request.Headers.Add("Cookie", $"{accessCookie}; {csrfCookie}");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        return await client.SendAsync(request);
    }

    private static async Task<(string Token, string Cookie)> GetCsrfAsync(HttpClient client, string accessCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");
        request.Headers.Add("Cookie", accessCookie);
        var response = await client.SendAsync(request);

        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith("commitahead_csrf=", StringComparison.Ordinal));
        var body = await response.Content.ReadFromJsonAsync<CsrfToken>();

        return (body!.Token, setCookie[..setCookie.IndexOf(';')]);
    }

    private sealed record CsrfToken(string Token);
}
