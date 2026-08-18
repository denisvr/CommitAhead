using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommitAhead.Api.Tests.Auth;
using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Api.Tests.TestInfrastructure;

/// <summary>
/// Shared setup for every Postgres-backed Api.Tests endpoint test: a provisioned, enabled user's
/// session cookie plus the CSRF cookie/header pair every mutating request needs (see CsrfTests for
/// the same pattern applied to the auth endpoints).
/// </summary>
internal static class PostgresApiTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<(HttpClient Client, string AccessCookie)> CreateAuthenticatedClientAsync(this PostgresApiTestFactory factory, Guid userId)
    {
        var supabaseSub = $"sub-{userId}";

        // Every owner-scoped table has a real FK to users.id (Group A of the Phase 1 corrective
        // pass) — the owner must be a genuine row in the same Testcontainers database, not an
        // in-memory stand-in, or every insert in these tests would fail with a FK violation.
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(factory.ConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(userId, supabaseSub, $"{supabaseSub}@example.com", DateTime.UtcNow), CancellationToken.None);

        var token = JwtTestTokenFactory.CreateAccessToken(supabaseSub);

        // HandleCookies=false: every request in these tests carries its own explicit Cookie
        // header (access token, and the CSRF cookie fetched fresh per mutation below). With the
        // default cookie-handling client, the underlying CookieContainer silently consumes
        // Set-Cookie response headers instead of surfacing them, so a second /auth/csrf fetch on
        // the same client throws "the given header was not found" for Set-Cookie.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return (client, $"commitahead_access={token}");
    }

    public static async Task<HttpResponseMessage> SendGetAsync(this HttpClient client, string url, string accessCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", accessCookie);
        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendMutatingAsync(this HttpClient client, HttpMethod method, string url, string accessCookie)
        => SendMutatingAsync<object?>(client, method, url, accessCookie, null);

    public static async Task<HttpResponseMessage> SendMutatingAsync<TBody>(this HttpClient client, HttpMethod method, string url, string accessCookie, TBody body)
    {
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client, accessCookie);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", $"{accessCookie}; {csrfCookie}");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await client.SendAsync(request);
    }

    /// <summary>For multipart/form-data requests (e.g. file uploads) — JsonContent.Create in SendMutatingAsync above only fits a JSON body.</summary>
    public static async Task<HttpResponseMessage> SendMultipartAsync(this HttpClient client, HttpMethod method, string url, string accessCookie, MultipartFormDataContent content)
    {
        var (csrfToken, csrfCookie) = await GetCsrfTokenAsync(client, accessCookie);

        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add("Cookie", $"{accessCookie}; {csrfCookie}");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        return await client.SendAsync(request);
    }

    private static async Task<(string Token, string Cookie)> GetCsrfTokenAsync(HttpClient client, string accessCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/csrf");
        request.Headers.Add("Cookie", accessCookie);
        var response = await client.SendAsync(request);

        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("commitahead_csrf=", StringComparison.Ordinal));
        var cookiePair = setCookie[..setCookie.IndexOf(';')];
        var body = await response.Content.ReadFromJsonAsync<CsrfResponseDto>();
        return (body!.Token, cookiePair);
    }

    private sealed record CsrfResponseDto(string Token);
}
