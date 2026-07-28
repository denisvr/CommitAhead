using System.Net;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Api.Tests.Auth;

public class MeEndpointTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;

    public MeEndpointTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Users.Clear();
    }

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithValidTokenForEnabledUser_ReturnsTheirEmail()
    {
        _factory.Users.Add(new User(Guid.NewGuid(), "sub-enabled", "owner@example.com", DateTime.UtcNow));
        var token = JwtTestTokenFactory.CreateAccessToken("sub-enabled");
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"commitahead_access={token}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("owner@example.com", body);
    }

    [Fact]
    public async Task Get_WithValidTokenForUnknownSub_ReturnsForbidden()
    {
        var token = JwtTestTokenFactory.CreateAccessToken("sub-unknown");
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"commitahead_access={token}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithValidTokenForDisabledUser_ReturnsForbidden()
    {
        var user = new User(Guid.NewGuid(), "sub-disabled", "owner@example.com", DateTime.UtcNow);
        user.Disable();
        _factory.Users.Add(user);
        var token = JwtTestTokenFactory.CreateAccessToken("sub-disabled");
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"commitahead_access={token}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithExpiredToken_ReturnsUnauthorized()
    {
        _factory.Users.Add(new User(Guid.NewGuid(), "sub-enabled", "owner@example.com", DateTime.UtcNow));
        var token = JwtTestTokenFactory.CreateAccessToken("sub-enabled", DateTime.UtcNow.AddMinutes(-10));
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"commitahead_access={token}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithTokenOlderThanFifteenMinutesByIssuedAt_ReturnsUnauthorized_EvenWhenNotExpired()
    {
        _factory.Users.Add(new User(Guid.NewGuid(), "sub-enabled", "owner@example.com", DateTime.UtcNow));
        // exp still valid (default +15min from now), but iat is 20 minutes old — the effective
        // 15-minute access token limit is enforced independently of Supabase's own exp claim.
        var token = JwtTestTokenFactory.CreateAccessToken("sub-enabled", issuedAtUtc: DateTime.UtcNow.AddMinutes(-20));
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", $"commitahead_access={token}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
