using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.InterviewNotes;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Api.Tests.InterviewNotes;

[Collection(StudyItemsApiCollection.Name)]
public class InterviewNotesEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public InterviewNotesEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateInterviewNoteRequest ValidCreateRequest(Guid? jobAnalysisId = null) => new(
        "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15), ["Q1"], ["Gap1"], ["Lesson1"], jobAnalysisId);

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/interview-notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoNotesYet_ReturnsEmptyList()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/interview-notes", accessCookie);
        var results = await response.Content.ReadFromJsonAsync<List<InterviewNoteResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Empty(results!);
    }

    [Fact]
    public async Task GetById_WithNoSuchNote_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/interview-notes/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGetById_RoundTripsTheNote()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/interview-notes", accessCookie, ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<InterviewNoteCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/interview-notes/{created!.Id}", accessCookie);
        var note = await getResponse.Content.ReadFromJsonAsync<InterviewNoteResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("Acme Corp", note!.Company);
        Assert.Equal(["Q1"], note.Questions);
        Assert.Null(note.JobAnalysisId);
    }

    [Fact]
    public async Task Post_ReferencingTheCallersOwnJobAnalysis_CreatesANoteReferencingIt()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var analysisPostResponse = await client.SendMutatingAsync(
            HttpMethod.Post, "/api/job-analyses", accessCookie, new CreateJobAnalysisRequest("Title", "Job posting text.", null));
        var analysisCreated = await analysisPostResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/interview-notes", accessCookie, ValidCreateRequest(analysisCreated!.Id));
        var created = await postResponse.Content.ReadFromJsonAsync<InterviewNoteCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/interview-notes/{created!.Id}", accessCookie);
        var note = await getResponse.Content.ReadFromJsonAsync<InterviewNoteResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(analysisCreated.Id, note!.JobAnalysisId);
    }

    [Fact]
    public async Task Post_ReferencingAJobAnalysisTheCallerDoesNotOwn_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, "/api/interview-notes", accessCookie, ValidCreateRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTheCompany()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/interview-notes", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<InterviewNoteCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/interview-notes/{created!.Id}", accessCookie,
            new UpdateInterviewNoteRequest("New Corp", "Backend Engineer", InterviewRound.Behavioral, 2, null, new DateOnly(2026, 2, 1), ["Q2"], ["Gap2"], ["Lesson2"], null));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/interview-notes/{created.Id}", accessCookie);
        var note = await getResponse.Content.ReadFromJsonAsync<InterviewNoteResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("New Corp", note!.Company);
    }

    [Fact]
    public async Task Put_WithNoSuchNote_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/interview-notes/{Guid.NewGuid()}", accessCookie,
            new UpdateInterviewNoteRequest("Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15), ["Q1"], ["Gap1"], ["Lesson1"], null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheNote()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/interview-notes", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<InterviewNoteCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/interview-notes/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/interview-notes/{created.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNoSuchNote_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/interview-notes/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
