using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Api.Tests.StudyItems;

[Collection(StudyItemsApiCollection.Name)]
public class StudyQueueEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public StudyQueueEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateStudyItemRequest Request(string title, int importance, int initialMastery) => new(
        title,
        StudyItemCategory.Theory,
        importance,
        initialMastery,
        Tags: [],
        Details: new TheoryDetailsDto("s", [], [], []));

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/study-queue");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_OrdersByEffectiveScoreDescending_AndOmitsOtherOwnersItems()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, Request("Low priority", importance: 1, initialMastery: 5));
        await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, Request("High priority", importance: 5, initialMastery: 1));

        var (otherClient, otherCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());
        await otherClient.SendMutatingAsync(HttpMethod.Post, "/api/study-items", otherCookie, Request("Someone else's item", importance: 5, initialMastery: 1));

        var response = await client.SendGetAsync("/api/study-queue", accessCookie);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var queue = await response.Content.ReadFromJsonAsync<List<RankedStudyItemResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.NotNull(queue);
        Assert.Equal(["High priority", "Low priority"], queue.Select(i => i.Title));
        Assert.True(queue[0].EffectiveScore > queue[1].EffectiveScore);
    }

    [Fact]
    public async Task Get_ExcludesArchivedItems()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());
        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", accessCookie, Request("Archived item", importance: 5, initialMastery: 1));
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        await client.SendMutatingAsync(HttpMethod.Post, $"/api/study-items/{created!.Id}/archive", accessCookie);

        var response = await client.SendGetAsync("/api/study-queue", accessCookie);
        var queue = await response.Content.ReadFromJsonAsync<List<RankedStudyItemResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Empty(queue!);
    }
}
