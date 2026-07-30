using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommitAhead.Api.Tests.Auth;
using CommitAhead.Domain.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CommitAhead.Api.Tests.StudyItems;

/// <summary>
/// Shared setup for every StudyItems endpoint test: a provisioned, enabled user's session cookie
/// plus the CSRF cookie/header pair every mutating request needs (see CsrfTests for the same
/// pattern applied to the auth endpoints).
/// </summary>
internal static class StudyItemsApiTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static (HttpClient Client, string AccessCookie) CreateAuthenticatedClient(this StudyItemsTestWebApplicationFactory factory, Guid userId)
    {
        var supabaseSub = $"sub-{userId}";
        factory.Users.Add(new User(userId, supabaseSub, $"{supabaseSub}@example.com", DateTime.UtcNow));
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
