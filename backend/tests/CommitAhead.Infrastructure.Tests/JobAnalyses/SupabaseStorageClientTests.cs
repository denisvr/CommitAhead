using System.Net;
using System.Text;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Tests.Auth;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

public class SupabaseStorageClientTests
{
    private static readonly Uri BaseAddress = new("https://project.supabase.co");

    [Fact]
    public async Task UploadAsync_PostsToTheObjectEndpoint_WithApikeyAndTheCurrentUsersBearerToken()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler, "user-jwt-1");
        var content = Encoding.ASCII.GetBytes("%PDF-fake-content");

        await client.UploadAsync("owner-id/object-id", new MemoryStream(content), "application/pdf", CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/storage/v1/object/job-postings/owner-id/object-id", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("test-anon-key", handler.LastRequest.Headers.GetValues("apikey").Single());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("user-jwt-1", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal("application/pdf", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
        Assert.Equal(content, Encoding.ASCII.GetBytes(handler.LastRequestBody!));
    }

    [Fact]
    public async Task DeleteAsync_DeletesTheBucketEndpoint_WithAPrefixesJsonBody()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler, "user-jwt-2");

        await client.DeleteAsync("owner-id/object-id", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("/storage/v1/object/job-postings", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("user-jwt-2", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Contains("\"prefixes\":[\"owner-id/object-id\"]", handler.LastRequestBody);
    }

    [Fact]
    public async Task UploadAsync_WhenSupabaseReturnsAnError_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(handler, "user-jwt-3");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.UploadAsync("owner-id/object-id", new MemoryStream([1, 2, 3]), "application/pdf", CancellationToken.None));
    }

    /// <summary>Two calls made "by" two different users must each carry their own token — never a client-default header that could leak one user's token onto another's pooled-client call.</summary>
    [Fact]
    public async Task TwoCallsByDifferentUsers_EachCarryTheirOwnBearerToken_NeverAClientDefault()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = BaseAddress };
        httpClient.DefaultRequestHeaders.Add("apikey", "test-anon-key");

        await new SupabaseStorageClient(httpClient, new FakeCurrentUserAccessToken { Value = "jwt-owner-a" })
            .DeleteAsync("owner-a/object-1", CancellationToken.None);
        Assert.Equal("jwt-owner-a", handler.LastRequest!.Headers.Authorization?.Parameter);

        await new SupabaseStorageClient(httpClient, new FakeCurrentUserAccessToken { Value = "jwt-owner-b" })
            .DeleteAsync("owner-b/object-2", CancellationToken.None);
        Assert.Equal("jwt-owner-b", handler.LastRequest!.Headers.Authorization?.Parameter);
        Assert.False(httpClient.DefaultRequestHeaders.Contains("Authorization"));
    }

    private static SupabaseStorageClient CreateClient(HttpMessageHandler handler, string accessToken)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseAddress };
        httpClient.DefaultRequestHeaders.Add("apikey", "test-anon-key");
        return new SupabaseStorageClient(httpClient, new FakeCurrentUserAccessToken { Value = accessToken });
    }
}
