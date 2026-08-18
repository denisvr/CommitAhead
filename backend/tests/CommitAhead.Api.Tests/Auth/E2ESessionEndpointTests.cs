using System.IdentityModel.Tokens.Jwt;
using System.Net;
using CommitAhead.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Proves the E2E-only session endpoint (docs/testing/strategy.md §7.3) is unreachable outside
/// the E2E environment, is absent from the generated OpenAPI document, and — inside E2E — mints a
/// session with the exact claims and cookies the real callback produces. The E2E-success test
/// injects configuration via <see cref="IWebHostBuilder.UseSetting"/> rather than process
/// environment variables: `UseSetting` applies directly to the same builder Program.cs's own
/// `WebApplication.CreateBuilder(args)` returns, before E2EConfigurationGuard.Validate ever runs —
/// unlike environment variables, which are process-global and would leak into every other test
/// running concurrently in a different xunit collection.
/// </summary>
public sealed class E2ESessionEndpointTests
{
    private const string SigningKeySentinel = "e2e-test-signing-key-at-least-32-bytes-long";
    private const string IssuerSentinel = "https://e2e.commitahead.local/auth/v1";
    private const string SupabaseUserIdSentinel = "e2e-user";

    private static readonly Dictionary<string, string?> E2ESentinelSettings = new()
    {
        ["E2E:SigningKey"] = SigningKeySentinel,
        ["E2E:Issuer"] = IssuerSentinel,
        ["E2E:SupabaseUserId"] = SupabaseUserIdSentinel,
        ["Supabase:Url"] = E2EConfigurationGuard.SupabaseUrlSentinel,
        ["Supabase:AnonKey"] = E2EConfigurationGuard.SupabaseAnonKeySentinel,
        ["Auth:CallbackUrl"] = E2EConfigurationGuard.AuthCallbackUrlSentinel,
    };

    [Theory]
    [InlineData("Development")]
    [InlineData("Docker")]
    [InlineData("Production")]
    public async Task Post_UnderNonE2EEnvironment_Returns404(string environmentName)
    {
        using var factory = new SingleEnvironmentFactory(environmentName, extraSettings: null);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/auth/e2e/session", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GeneratedOpenApiDocument_DoesNotContainTheE2ESessionEndpoint()
    {
        // Reads the same build-time-generated CommitAhead.Api.json that npm run generate:api
        // (and CI's contract-drift check) reads — not a live HTTP request: MapOpenApi() is only
        // ever mapped under IsDevelopment() in Program.cs, and the endpoint it maps requires
        // authentication like everything else under the fallback policy, so hitting it live would
        // test the auth pipeline, not whether this controller is excluded from the document.
        var documentPath = FindGeneratedOpenApiDocumentPath();
        var document = await File.ReadAllTextAsync(documentPath);

        Assert.DoesNotContain("/auth/e2e/session", document, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindGeneratedOpenApiDocumentPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "CommitAhead.Api", "obj", "CommitAhead.Api.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not locate the generated CommitAhead.Api.json OpenAPI document — build the solution first.");
    }

    [Fact]
    public async Task Post_UnderE2EEnvironment_SetsSessionCookiesWithExpectedJwtClaimsAndLifetime()
    {
        using var factory = new SingleEnvironmentFactory("E2E", E2ESentinelSettings);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/auth/e2e/session", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
        var cookies = setCookieHeaders!.ToList();

        var accessCookie = Assert.Single(cookies, c => c.StartsWith("commitahead_access=", StringComparison.Ordinal));
        Assert.Contains("HttpOnly", accessCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", accessCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", accessCookie, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(cookies, c => c.StartsWith("commitahead_refresh=", StringComparison.Ordinal));
        Assert.Contains(cookies, c => c.StartsWith("commitahead_session_started=", StringComparison.Ordinal));

        var accessToken = ExtractCookieValue(accessCookie, "commitahead_access");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        Assert.Equal(IssuerSentinel, jwt.Issuer);
        Assert.Equal("authenticated", Assert.Single(jwt.Audiences));
        Assert.Equal(SupabaseUserIdSentinel, jwt.Subject);
        Assert.NotEqual(default, jwt.IssuedAt);
        Assert.NotEqual(default, jwt.ValidFrom);
        Assert.True(jwt.ValidFrom <= DateTime.UtcNow.AddSeconds(5), "nbf must not be in the future.");
        Assert.True(jwt.ValidTo <= jwt.IssuedAt.AddMinutes(15), "exp must be within the 15-minute effective lifetime cap.");
        Assert.True(jwt.ValidTo > jwt.IssuedAt, "exp must be after iat.");
    }

    private static string ExtractCookieValue(string setCookieHeader, string cookieName)
    {
        var firstSegment = setCookieHeader.Split(';')[0];
        var prefix = $"{cookieName}=";
        Assert.StartsWith(prefix, firstSegment, StringComparison.Ordinal);
        return firstSegment[prefix.Length..];
    }

    /// <summary>Fixes ASPNETCORE_ENVIRONMENT and, when provided, layers extra configuration keys
    /// on top via UseSetting — no repository/Supabase stubbing, since /auth/e2e/session and the
    /// 404/OpenAPI checks above never touch the database or a real Supabase call.</summary>
    private sealed class SingleEnvironmentFactory : WebApplicationFactory<Program>
    {
        private readonly string _environmentName;
        private readonly Dictionary<string, string?>? _extraSettings;

        public SingleEnvironmentFactory(string environmentName, Dictionary<string, string?>? extraSettings)
        {
            _environmentName = environmentName;
            _extraSettings = extraSettings;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);

            if (_extraSettings is not null)
            {
                foreach (var (key, value) in _extraSettings)
                {
                    builder.UseSetting(key, value);
                }
            }

            builder.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider());
        }
    }
}
