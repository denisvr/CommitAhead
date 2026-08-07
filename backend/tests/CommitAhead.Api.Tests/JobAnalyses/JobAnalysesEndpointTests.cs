using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;

namespace CommitAhead.Api.Tests.JobAnalyses;

[Collection(StudyItemsApiCollection.Name)]
public class JobAnalysesEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public JobAnalysesEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateJobAnalysisRequest ValidCreateRequest() => new("Senior Backend Engineer", "Job posting text.", "Some notes.");

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/job-analyses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoAnalysesYet_ReturnsEmptyList()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/job-analyses", accessCookie);
        var results = await response.Content.ReadFromJsonAsync<List<JobAnalysisResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Empty(results!);
    }

    [Fact]
    public async Task GetById_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/job-analyses/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGetById_RoundTripsThePastedTextJobSource()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created!.Id}", accessCookie);
        var analysis = await getResponse.Content.ReadFromJsonAsync<JobAnalysisResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("Senior Backend Engineer", analysis!.Title);
        var pastedText = Assert.IsType<PastedTextResponse>(analysis.JobSource);
        Assert.Equal("Job posting text.", pastedText.Content);
        Assert.Empty(analysis.Requirements);
        Assert.Empty(analysis.Gaps);
    }

    [Fact]
    public async Task Put_UpdatesTheTitle()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/job-analyses/{created!.Id}", accessCookie, new UpdateJobAnalysisRequest("New title", null));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created.Id}", accessCookie);
        var analysis = await getResponse.Content.ReadFromJsonAsync<JobAnalysisResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("New title", analysis!.Title);
    }

    [Fact]
    public async Task Put_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/job-analyses/{Guid.NewGuid()}", accessCookie, new UpdateJobAnalysisRequest("Title", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheAnalysis()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/job-analyses/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/job-analyses/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
