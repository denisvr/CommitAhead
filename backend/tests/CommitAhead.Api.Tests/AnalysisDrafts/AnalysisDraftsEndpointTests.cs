using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Api.Tests.AnalysisDrafts;

[Collection(StudyItemsApiCollection.Name)]
public class AnalysisDraftsEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public AnalysisDraftsEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateJobAnalysisRequest ValidCreateRequest() => new("Senior Backend Engineer", "Job posting text.", null);

    private async Task<(HttpClient Client, string AccessCookie, Guid DraftId)> CreateAnalyzedJobAnalysisAsync()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var analyzeResponse = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{Guid.NewGuid()}"));
        var analyzeBody = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeCommandResponse>(StudyItemsApiTestHelpers.JsonOptions);

        return (client, accessCookie, analyzeBody!.AnalysisDraftId!.Value);
    }

    private static ApplyAnalysisDraftRequest EmptyDecisions() => new([], [], []);

    [Fact]
    public async Task GetById_ForAnAnalyzedJobAnalysis_ReturnsThePendingDraftWithItsProposals()
    {
        var (client, accessCookie, draftId) = await CreateAnalyzedJobAnalysisAsync();

        var response = await client.SendGetAsync($"/api/analysis-drafts/{draftId}", accessCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisDraftResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(draftId, body!.Id);
        Assert.Equal(AnalysisDraftStatus.Pending, body.Status);
    }

    [Fact]
    public async Task GetById_WithNoSuchDraft_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/analysis-drafts/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherOwnersDraft_ReturnsNotFound()
    {
        var (_, _, draftId) = await CreateAnalyzedJobAnalysisAsync();
        var (otherClient, otherAccessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await otherClient.SendGetAsync($"/api/analysis-drafts/{draftId}", otherAccessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Apply_WithNoProposalsToDecide_ReturnsNoContent()
    {
        var (client, accessCookie, draftId) = await CreateAnalyzedJobAnalysisAsync();

        var response = await client.SendMutatingAsync(HttpMethod.Post, $"/api/analysis-drafts/{draftId}/apply", accessCookie, EmptyDecisions());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Apply_WithNoSuchDraft_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, $"/api/analysis-drafts/{Guid.NewGuid()}/apply", accessCookie, EmptyDecisions());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Apply_AppliedTwice_ReturnsConflictWithAStableOutcomeCode()
    {
        var (client, accessCookie, draftId) = await CreateAnalyzedJobAnalysisAsync();
        var first = await client.SendMutatingAsync(HttpMethod.Post, $"/api/analysis-drafts/{draftId}/apply", accessCookie, EmptyDecisions());
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.SendMutatingAsync(HttpMethod.Post, $"/api/analysis-drafts/{draftId}/apply", accessCookie, EmptyDecisions());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal(nameof(ApplyAnalysisDraftOutcome.DraftNotPending), problem!.Extensions["outcomeCode"]!.ToString());
    }

    [Fact]
    public async Task Apply_WithAMissingDecisionForAnExistingProposal_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var analyzeResponse = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{Guid.NewGuid()}"));
        var analyzeBody = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeCommandResponse>(StudyItemsApiTestHelpers.JsonOptions);

        // A decision for a proposal Id that doesn't exist on this (zero-proposal) draft is exactly
        // the "unknown proposal Id" case ApplyAnalysisDraftValidationException covers — proving the
        // widened ValidationExceptionFilter maps it to 422, not a 500.
        var request = new ApplyAnalysisDraftRequest([new SuggestionProposalDecision(Guid.NewGuid(), true, "{}")], [], []);
        var response = await client.SendMutatingAsync(HttpMethod.Post, $"/api/analysis-drafts/{analyzeBody!.AnalysisDraftId}/apply", accessCookie, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
