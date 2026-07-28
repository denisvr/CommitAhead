using System.Net;
using System.Net.Http.Json;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Api.Tests.Auth;

// Deliberately not IClassFixture: /auth/login is rate-limited (5 requests / 15 min per IP, see
// SecurityServiceCollectionExtensions), so sharing one host across every test in this class would
// exhaust the limit and produce 503s unrelated to the behavior under test. A fresh host per test
// gives each one its own limiter state.
public class LoginEndpointTests
{
    private const string GenericMessage = "If that email is registered, a sign-in link has been sent.";

    [Fact]
    public async Task Post_WithProvisionedEnabledEmail_ReturnsGenericMessage_AndInitiatesMagicLink()
    {
        using var factory = new AuthTestWebApplicationFactory();
        factory.Users.Add(new User(Guid.NewGuid(), "sub-enabled", "owner@example.com", DateTime.UtcNow));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "owner@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(GenericMessage, body!.Message);
        Assert.True(factory.SupabaseAuth.MagicLinkInitiated);
    }

    [Fact]
    public async Task Post_WithUnknownEmail_ReturnsTheSameGenericMessage_ButNeverCallsSupabase()
    {
        using var factory = new AuthTestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "unknown@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(GenericMessage, body!.Message);
        Assert.False(factory.SupabaseAuth.MagicLinkInitiated);
    }

    [Fact]
    public async Task Post_WithDisabledUserEmail_ReturnsTheSameGenericMessage_ButNeverCallsSupabase()
    {
        using var factory = new AuthTestWebApplicationFactory();
        var user = new User(Guid.NewGuid(), "sub-disabled", "disabled@example.com", DateTime.UtcNow);
        user.Disable();
        factory.Users.Add(user);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "disabled@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(GenericMessage, body!.Message);
        Assert.False(factory.SupabaseAuth.MagicLinkInitiated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public async Task Post_WithMalformedEmail_ReturnsBadRequest(string email)
    {
        using var factory = new AuthTestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(factory.SupabaseAuth.MagicLinkInitiated);
    }

    [Fact]
    public async Task Post_WithEmailLongerThanMaxLength_ReturnsBadRequest()
    {
        using var factory = new AuthTestWebApplicationFactory();
        var tooLong = new string('a', 316) + "@a.co"; // 321 chars total, one over MaxEmailLength
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record LoginResponse(string Message);
}
