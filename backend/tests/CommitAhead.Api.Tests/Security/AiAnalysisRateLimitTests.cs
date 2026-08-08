using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// ADR-0019's "ai-analysis" rate-limit policy — 10 AnalyzeX requests/hour per authenticated owner.
/// Uses a freshly generated owner Id (never reused by any other test in this collection), which
/// already isolates this test's rate-limiter bucket without needing a second WebApplicationFactory/
/// Testcontainers instance just for this one test.
/// </summary>
[Collection(StudyItemsApiCollection.Name)]
public class AiAnalysisRateLimitTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public AiAnalysisRateLimitTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Analyze_TheEleventhRequestWithinAnHour_Returns429WithRetryAfter()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(
            HttpMethod.Post, "/api/job-analyses", accessCookie, new CreateJobAnalysisRequest("Senior Backend Engineer", "Job posting text.", null));
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        for (var i = 0; i < 10; i++)
        {
            var response = await client.SendMutatingAsync(
                HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{i}-{Guid.NewGuid()}"));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var eleventh = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-10-{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
        Assert.True(eleventh.Headers.RetryAfter is not null || eleventh.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Apply_IsNeverRateLimited_EvenAfterExhaustingTheAnalyzeQuota()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(
            HttpMethod.Post, "/api/job-analyses", accessCookie, new CreateJobAnalysisRequest("Senior Backend Engineer", "Job posting text.", null));
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        for (var i = 0; i < 11; i++)
        {
            await client.SendMutatingAsync(HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{i}-{Guid.NewGuid()}"));
        }

        var applyResponse = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/analysis-drafts/{Guid.NewGuid()}/apply", accessCookie, new ApplyAnalysisDraftRequest([], [], []));

        Assert.NotEqual(HttpStatusCode.TooManyRequests, applyResponse.StatusCode);
    }
}
