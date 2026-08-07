using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CommitAhead.Application.Identity;
using CommitAhead.Application.JobAnalyses;

namespace CommitAhead.Infrastructure.JobAnalyses;

/// <summary>
/// HttpClient-based IJobPostingStorage against Supabase Storage's REST API. Every call carries the
/// project's anon key via the "apikey" header (as SupabaseAuthClient already does for GoTrue) and
/// the current request's own user JWT via "Authorization: Bearer" — built per HttpRequestMessage,
/// never as an HttpClient default header, since the typed client instance is pooled/reused across
/// different users' requests by IHttpClientFactory (ADR-0018: no service-role key, ever, for
/// runtime Storage calls).
/// </summary>
public sealed class SupabaseStorageClient : IJobPostingStorage
{
    private const string BucketName = "job-postings";

    private readonly HttpClient _httpClient;
    private readonly ICurrentUserAccessToken _accessToken;

    public SupabaseStorageClient(HttpClient httpClient, ICurrentUserAccessToken accessToken)
    {
        _httpClient = httpClient;
        _accessToken = accessToken;
    }

    public async Task UploadAsync(string storageObjectKey, Stream content, string mimeType, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{BucketName}/{EscapeKey(storageObjectKey)}")
        {
            Content = new StreamContent(content),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken.Value) },
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string storageObjectKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"storage/v1/object/{BucketName}")
        {
            Content = JsonContent.Create(new DeleteRequest([storageObjectKey])),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken.Value) },
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string EscapeKey(string storageObjectKey)
        => string.Join('/', storageObjectKey.Split('/').Select(Uri.EscapeDataString));

    private sealed record DeleteRequest([property: JsonPropertyName("prefixes")] string[] Prefixes);
}
