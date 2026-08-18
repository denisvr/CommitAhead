using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.CVPresentations;
using CommitAhead.Api.Features.ProfessionalProfiles;
using CommitAhead.Api.Tests.TestInfrastructure;
using UglyToad.PdfPig;

namespace CommitAhead.Api.Tests.CVPresentations;

[Collection(PostgresApiCollection.Name)]
public class CVPresentationEndpointTests
{
    private readonly PostgresApiTestFactory _factory;

    public CVPresentationEndpointTests(PostgresApiTestFactory factory)
    {
        _factory = factory;
    }

    private static ContactInfoDto ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static CreateProfessionalProfileRequest ValidProfileRequest() => new(ValidContactInfo(), "Backend engineer.");

    private static CreateCVPresentationRequest ValidCreateRequest(Guid professionalProfileId) => new(
        professionalProfileId, "UK — Senior Backend Engineer", "United Kingdom", "Senior Backend Engineer",
        "en-GB", "modern-one-page", null, false, true, true, false, "dd MMM yyyy", 2);

    private async Task<(System.Net.Http.HttpClient Client, string AccessCookie, Guid ProfessionalProfileId)> CreateAuthenticatedClientWithProfileAsync()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidProfileRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<ProfessionalProfileCreatedResponse>(PostgresApiTestHelpers.JsonOptions);
        return (client, accessCookie, created!.Id);
    }

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cv-presentations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoPresentationsYet_ReturnsEmptyList()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/cv-presentations", accessCookie);
        var results = await response.Content.ReadFromJsonAsync<List<CVPresentationResponse>>(PostgresApiTestHelpers.JsonOptions);

        Assert.Empty(results!);
    }

    [Fact]
    public async Task GetById_WithNoSuchPresentation_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/cv-presentations/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGetById_RoundTripsThePresentationWithDefaultedProfileLinkSelections()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var link = new ProfileLinkDto(Guid.NewGuid(), Domain.ProfessionalProfiles.ProfileLinkKind.GitHub, null, "https://github.com/ada");
        await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/profile-links", accessCookie, new[] { link });

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/cv-presentations/{created!.Id}", accessCookie);
        var presentation = await getResponse.Content.ReadFromJsonAsync<CVPresentationResponse>(PostgresApiTestHelpers.JsonOptions);
        Assert.Equal("UK — Senior Backend Engineer", presentation!.Label);
        Assert.Equal(profileId, presentation.ProfessionalProfileId);
        Assert.Equal([link.Id], presentation.ProfileLinkSelections);
    }

    [Fact]
    public async Task Post_ReferencingAProfileTheCallerDoesNotOwn_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTheLabel()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/cv-presentations/{created!.Id}", accessCookie,
            new UpdateCVPresentationRequest("Germany — Backend Engineer", "Germany", null, "de-DE", "modern-one-page", null, false, true, false, false, "dd.MM.yyyy", 1));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/cv-presentations/{created.Id}", accessCookie);
        var presentation = await getResponse.Content.ReadFromJsonAsync<CVPresentationResponse>(PostgresApiTestHelpers.JsonOptions);
        Assert.Equal("Germany — Backend Engineer", presentation!.Label);
    }

    [Fact]
    public async Task Put_WithNoSuchPresentation_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/cv-presentations/{Guid.NewGuid()}", accessCookie,
            new UpdateCVPresentationRequest("Label", "Market", null, "en-GB", "modern-one-page", null, false, true, false, false, "dd MMM yyyy", 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesThePresentation()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/cv-presentations/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/cv-presentations/{created.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PutExperienceSelections_ThenGetById_RoundTripsTheSelection()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var entry = new ExperienceEntryDto(
            Guid.NewGuid(), "Acme", null, "Engineer", Domain.ProfessionalProfiles.EmploymentType.Permanent,
            new YearMonthDto(2020, 1), null, null, Domain.ProfessionalProfiles.WorkMode.Remote, "Summary", [], []);
        await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/experience", accessCookie, new[] { entry });
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/cv-presentations/{created!.Id}/experience-selections", accessCookie, new[] { entry.Id });
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/cv-presentations/{created.Id}", accessCookie);
        var presentation = await getResponse.Content.ReadFromJsonAsync<CVPresentationResponse>(PostgresApiTestHelpers.JsonOptions);
        Assert.Equal([entry.Id], presentation!.ExperienceSelections);
    }

    [Fact]
    public async Task PutExperienceSelections_ReferencingAnEntryNotOnTheProfile_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/cv-presentations/{created!.Id}/experience-selections", accessCookie, new[] { Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PutExperienceSelections_WithNoSuchPresentation_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/cv-presentations/{Guid.NewGuid()}/experience-selections", accessCookie, Array.Empty<Guid>());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/cv-presentations/{Guid.NewGuid()}/export");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Export_WithNoSuchPresentation_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/cv-presentations/{Guid.NewGuid()}/export", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_ForAValidPresentation_ReturnsAParseablePdfContainingTheContactNameAndTargetRole()
    {
        var (client, accessCookie, profileId) = await CreateAuthenticatedClientWithProfileAsync();
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/cv-presentations", accessCookie, ValidCreateRequest(profileId));
        var created = await postResponse.Content.ReadFromJsonAsync<CVPresentationCreatedResponse>(PostgresApiTestHelpers.JsonOptions);

        var response = await client.SendGetAsync($"/api/cv-presentations/{created!.Id}/export", accessCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        using var pdf = PdfDocument.Open(pdfBytes);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        Assert.Contains("Ada Lovelace", text);
        Assert.Contains("Senior Backend Engineer", text);
    }
}
