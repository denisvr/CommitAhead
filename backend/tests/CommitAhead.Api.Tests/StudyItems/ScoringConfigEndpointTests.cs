using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.StudyItems;

namespace CommitAhead.Api.Tests.StudyItems;

[Collection(StudyItemsApiCollection.Name)]
public class ScoringConfigEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public ScoringConfigEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/scoring-config");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoOverride_ReturnsDefaultsAndIsOverriddenFalse()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/scoring-config", accessCookie);
        var config = await response.Content.ReadFromJsonAsync<ScoringConfigResponse>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Equal(40, config!.ImportanceWeight);
        Assert.Equal(35, config.DemandWeight);
        Assert.Equal(25, config.MasteryGapWeight);
        Assert.False(config.IsOverridden);
    }

    [Fact]
    public async Task Put_ThenGet_RoundTripsTheOverride()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var putResponse = await client.SendMutatingAsync(HttpMethod.Put, "/api/scoring-config", accessCookie, new UpdateScoringConfigRequest(50, 30, 20));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync("/api/scoring-config", accessCookie);
        var config = await getResponse.Content.ReadFromJsonAsync<ScoringConfigResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(50, config!.ImportanceWeight);
        Assert.True(config.IsOverridden);
    }

    [Fact]
    public async Task Put_WithWeightsNotSummingTo100_ReturnsBadRequest()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Put, "/api/scoring-config", accessCookie, new UpdateScoringConfigRequest(50, 30, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesAnExistingOverride()
    {
        var (client, accessCookie) = _factory.CreateAuthenticatedClient(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Put, "/api/scoring-config", accessCookie, new UpdateScoringConfigRequest(50, 30, 20));

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, "/api/scoring-config", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync("/api/scoring-config", accessCookie);
        var config = await getResponse.Content.ReadFromJsonAsync<ScoringConfigResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.False(config!.IsOverridden);
    }
}
