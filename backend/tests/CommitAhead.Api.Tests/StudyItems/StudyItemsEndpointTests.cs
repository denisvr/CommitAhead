using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Api.Tests.StudyItems;

[Collection(StudyItemsApiCollection.Name)]
public class StudyItemsEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public StudyItemsEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateStudyItemRequest ValidCreateRequest(string title = "CAP theorem") => new(
        title,
        StudyItemCategory.Theory,
        Importance: 3,
        InitialMastery: 2,
        Tags: ["distributed-systems"],
        Details: new TheoryDetailsDto("Summary", ["Key point"], ["What is CAP?"], []));

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/study-items/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutCsrfToken_IsRejected()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/study-items");
        request.Headers.Add("Cookie", accessCookie);
        request.Content = JsonContent.Create(ValidCreateRequest(), options: StudyItemsApiTestHelpers.JsonOptions);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGetById_RoundTripsTheItem()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Equal("CAP theorem", item!.Title);
        Assert.Equal(StudyItemCategory.Theory, item.Category);
        Assert.Equal(StudyItemStatus.Active, item.Status);
        Assert.Equal(["distributed-systems"], item.Tags);
        var details = Assert.IsType<TheoryDetailsDto>(item.Details);
        Assert.Equal("Summary", details.SummaryMarkdown);
        Assert.Empty(item.Reviews);
    }

    [Fact]
    public async Task GetById_SerializesEnumsAsStrings_NotNumbers()
    {
        // The frontend's OpenAPI-generated client depends on this: if enums serialized as plain
        // ints, generated-client callers would see raw numbers instead of "Theory"/"Active"/etc.
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created!.Id}", accessCookie);
        var rawJson = await getResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"category\":\"Theory\"", rawJson);
        Assert.Contains("\"status\":\"Active\"", rawJson);
    }

    [Fact]
    public async Task Post_WithBlankTitle_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest(title: "   "));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherOwnersItem_ReturnsNotFound()
    {
        var (ownerClient, ownerCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await ownerClient.SendMutatingAsync(HttpMethod.Post, "/api/study-items", ownerCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var (otherClient, otherCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var response = await otherClient.SendGetAsync($"/api/study-items/{created!.Id}", otherCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForNonexistentId_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/study-items/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTitleImportanceAndTags()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var updateRequest = new UpdateStudyItemRequest("CAP theorem, revisited", 5, ["distributed-systems", "consistency"], new TheoryDetailsDto("New summary", [], [], []));
        var updateResponse = await client.SendMutatingAsync(HttpMethod.Put, $"/api/study-items/{created!.Id}", accessCookie, updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("CAP theorem, revisited", item!.Title);
        Assert.Equal(5, item.Importance);
        Assert.Equal(["distributed-systems", "consistency"], item.Tags);
    }

    [Fact]
    public async Task Put_ForNonexistentId_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var updateRequest = new UpdateStudyItemRequest("Title", 3, [], new TheoryDetailsDto("s", [], [], []));

        var response = await client.SendMutatingAsync(HttpMethod.Put, $"/api/study-items/{Guid.NewGuid()}", accessCookie, updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Archive_SetsStatusToArchived()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var archiveResponse = await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created!.Id}/archive", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(StudyItemStatus.Archived, item!.Status);
    }

    [Fact]
    public async Task Restore_AfterArchive_SetsStatusBackToActive()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created!.Id}/archive", accessCookie);

        var restoreResponse = await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created.Id}/restore", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(StudyItemStatus.Active, item!.Status);
    }

    [Fact]
    public async Task Restore_ForNonexistentId_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{Guid.NewGuid()}/restore", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutStatusFilter_ReturnsActiveAndArchivedItems()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var activeResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest("Active item"));
        var active = await activeResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        var archivedResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest("Archived item"));
        var archived = await archivedResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{archived!.Id}/archive", accessCookie);

        var listResponse = await client.SendGetAsync("/api/study-items", accessCookie);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var items = await listResponse.Content.ReadFromJsonAsync<List<StudyItemSummaryResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Equal(2, items!.Count);
        Assert.Contains(items, i => i.Id == active!.Id && i.Status == StudyItemStatus.Active);
        Assert.Contains(items, i => i.Id == archived.Id && i.Status == StudyItemStatus.Archived);
    }

    [Fact]
    public async Task Get_WithActiveStatusFilter_ExcludesArchivedItems()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var activeResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest("Active item"));
        var active = await activeResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        var archivedResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest("Archived item"));
        var archived = await archivedResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{archived!.Id}/archive", accessCookie);

        var listResponse = await client.SendGetAsync("/api/study-items?status=Active", accessCookie);
        var items = await listResponse.Content.ReadFromJsonAsync<List<StudyItemSummaryResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Equal([active!.Id], items!.Select(i => i.Id));
    }

    [Fact]
    public async Task Get_IsScopedToOwner()
    {
        var (ownerClient, ownerCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await ownerClient.SendMutatingAsync(HttpMethod.Post, "/api/study-items", ownerCookie, ValidCreateRequest());

        var (otherClient, otherCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var listResponse = await otherClient.SendGetAsync("/api/study-items", otherCookie);
        var items = await listResponse.Content.ReadFromJsonAsync<List<StudyItemSummaryResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Empty(items!);
    }

    [Fact]
    public async Task Delete_WithNoReviews_Succeeds()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/study-items/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithReviews_ReturnsConflict()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created!.Id}/reviews", accessCookie, new SubmitStudyReviewRequest(4, "Went well"));

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/study-items/{created.Id}", accessCookie);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_AddsAReviewAndRecomputesMastery()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var reviewResponse = await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created!.Id}/reviews", accessCookie, new SubmitStudyReviewRequest(4, "Went well"));
        Assert.Equal(HttpStatusCode.NoContent, reviewResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Single(item!.Reviews);
        Assert.Equal(4, item.Reviews[0].ConfidenceRating);
        Assert.Equal(4m, item.Mastery);
    }

    [Fact]
    public async Task SetPriorityOverride_ThenGet_ShowsTheOverrideAsTheEffectiveScore()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var overrideResponse = await client.SendMutatingAsync(HttpMethod.Put, $"/api/study-items/{created!.Id}/priority-override", accessCookie, new SetPriorityOverrideRequest(95, "Interview next week"));
        Assert.Equal(HttpStatusCode.NoContent, overrideResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(95, item!.EffectiveScore);
        Assert.Equal(95, item.PriorityOverrideScore);
        Assert.Equal("Interview next week", item.PriorityOverrideReason);
    }

    [Fact]
    public async Task ClearPriorityOverride_RemovesIt()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Put, $"/api/study-items/{created!.Id}/priority-override", accessCookie, new SetPriorityOverrideRequest(95, "Interview next week"));

        var clearResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/study-items/{created.Id}/priority-override", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/study-items/{created.Id}", accessCookie);
        var item = await getResponse.Content.ReadFromJsonAsync<StudyItemResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Null(item!.PriorityOverrideScore);
    }
}
