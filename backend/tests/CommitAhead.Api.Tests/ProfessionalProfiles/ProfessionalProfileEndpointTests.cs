using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.ProfessionalProfiles;
using CommitAhead.Api.Tests.TestInfrastructure;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Api.Tests.ProfessionalProfiles;

[Collection(PostgresApiCollection.Name)]
public class ProfessionalProfileEndpointTests
{
    private readonly PostgresApiTestFactory _factory;

    public ProfessionalProfileEndpointTests(PostgresApiTestFactory factory)
    {
        _factory = factory;
    }

    private static ContactInfoDto ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static CreateProfessionalProfileRequest ValidCreateRequest() => new(ValidContactInfo(), "Backend engineer.");

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/professional-profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoProfileYet_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/professional-profile", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGet_RoundTripsTheProfile()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.SendGetAsync("/api/professional-profile", accessCookie);
        var profile = await getResponse.Content.ReadFromJsonAsync<ProfessionalProfileResponse>(PostgresApiTestHelpers.JsonOptions);
        Assert.Equal("Ada Lovelace", profile!.ContactInfo.Name);
        Assert.Equal("Backend engineer.", profile.SummaryMarkdown);
        Assert.Empty(profile.Experience);
    }

    [Fact]
    public async Task Post_WhenAlreadyExists_ReturnsConflict()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());

        var response = await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithBlankSummary_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, new CreateProfessionalProfileRequest(ValidContactInfo(), "   "));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesContactInfoAndSummary()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, "/api/professional-profile", accessCookie,
            new UpdateProfessionalProfileRequest(new ContactInfoDto("Grace Hopper", "grace@example.com", null, null, null), "New summary."));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync("/api/professional-profile", accessCookie);
        var profile = await getResponse.Content.ReadFromJsonAsync<ProfessionalProfileResponse>(PostgresApiTestHelpers.JsonOptions);
        Assert.Equal("Grace Hopper", profile!.ContactInfo.Name);
        Assert.Equal("New summary.", profile.SummaryMarkdown);
    }

    [Fact]
    public async Task Put_WithNoExistingProfile_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, "/api/professional-profile", accessCookie, new UpdateProfessionalProfileRequest(ValidContactInfo(), "Summary."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutExperience_ThenGet_RoundTripsTheEntry()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());
        var entry = new ExperienceEntryDto(
            Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonthDto(2020, 1), null, null, WorkMode.Remote, "Summary", ["Shipped v2"], []);

        var putResponse = await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/experience", accessCookie, new[] { entry });
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync("/api/professional-profile", accessCookie);
        var profile = await getResponse.Content.ReadFromJsonAsync<ProfessionalProfileResponse>(PostgresApiTestHelpers.JsonOptions);
        var roundTripped = Assert.Single(profile!.Experience);
        Assert.Equal("Acme", roundTripped.Company);
        Assert.Equal(2020, roundTripped.StartDate.Year);
    }

    [Fact]
    public async Task PutExperience_ReferencingANonexistentSkill_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());
        var entry = new ExperienceEntryDto(
            Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonthDto(2020, 1), null, null, WorkMode.Remote, "Summary", [], [Guid.NewGuid()]);

        var response = await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/experience", accessCookie, new[] { entry });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PutSkills_ThenPutExperienceReferencingIt_Succeeds()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        await client.SendMutatingAsync(HttpMethod.Post, "/api/professional-profile", accessCookie, ValidCreateRequest());
        var skill = new SkillDto(Guid.NewGuid(), "C#", "c", SkillCategory.Language, null);
        await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/skills", accessCookie, new[] { skill });
        var entry = new ExperienceEntryDto(
            Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonthDto(2020, 1), null, null, WorkMode.Remote, "Summary", [], [skill.Id]);

        var response = await client.SendMutatingAsync(HttpMethod.Put, "/api/professional-profile/experience", accessCookie, new[] { entry });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
