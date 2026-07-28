using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CommitAhead.Api.Tests.Routing;

/// <summary>
/// Regression coverage: the SPA fallback must not swallow unmatched /api or /auth requests into
/// index.html — they must 404. This also guards against a related trap: the secure-by-default
/// fallback authorization policy applies even to requests that match no endpoint at all, so an
/// unmatched /api or /auth route can silently become 401 instead of 404 unless the catch-all
/// endpoints handling them are explicitly [AllowAnonymous] (see Program.cs).
/// </summary>
public class SpaFallbackRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SpaFallbackRoutingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnmatchedApiRoute_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnmatchedAuthRoute_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnmatchedNonApiRoute_DoesNotReturnUnauthorized()
    {
        // This test host has no physical wwwroot/index.html (that only exists in a published
        // artifact — see the "combined-artifact" CI job for the full file-serving check), so the
        // fallback endpoint itself 404s here. The behavior this guards is that it must not be
        // 401 — a route outside /api and /auth reaching the SPA fallback is always anonymous.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/some-client-route");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
