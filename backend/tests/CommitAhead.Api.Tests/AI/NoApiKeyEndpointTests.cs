using System.Net;
using CommitAhead.Api.Tests.Auth;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Api.Tests.AI;

/// <summary>
/// Proves the fix for the eager-API-key-resolution bug: JobAnalyses/CVPresentations/InterviewNotes
/// controllers all constructor-inject an AnalyzeX use case, which constructor-injects IAIProvider —
/// with no Anthropic API key configured, ordinary GET/PUT/DELETE actions on these controllers must
/// still work, never 500 from a DI resolution failure. Uses NoApiKeyTestWebApplicationFactory
/// (the real AnthropicAIProvider registration path, not the FakeAIProvider override) precisely so
/// this bug would actually be caught.
/// </summary>
[Collection(NoApiKeyApiCollection.Name)]
public sealed class NoApiKeyEndpointTests
{
    private readonly NoApiKeyTestWebApplicationFactory _factory;

    public NoApiKeyEndpointTests(NoApiKeyTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string AccessCookie)> CreateAuthenticatedClientAsync(Guid userId)
    {
        var supabaseSub = $"sub-{userId}";
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_factory.ConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(userId, supabaseSub, $"{supabaseSub}@example.com", DateTime.UtcNow), CancellationToken.None);

        var token = JwtTestTokenFactory.CreateAccessToken(supabaseSub);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return (client, $"commitahead_access={token}");
    }

    [Theory]
    [InlineData("/api/job-analyses")]
    [InlineData("/api/cv-presentations")]
    [InlineData("/api/interview-notes")]
    public async Task Get_WithNoAnthropicApiKeyConfigured_Returns200NotAServerError(string listUrl)
    {
        var (client, accessCookie) = await CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync(listUrl, accessCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/job-analyses")]
    [InlineData("/api/cv-presentations")]
    [InlineData("/api/interview-notes")]
    public async Task GetById_WithNoAnthropicApiKeyConfigured_Returns404NotAServerError(string baseUrl)
    {
        var (client, accessCookie) = await CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"{baseUrl}/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/job-analyses")]
    [InlineData("/api/cv-presentations")]
    [InlineData("/api/interview-notes")]
    public async Task Delete_WithNoAnthropicApiKeyConfigured_Returns404NotAServerError(string baseUrl)
    {
        var (client, accessCookie) = await CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Delete, $"{baseUrl}/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
