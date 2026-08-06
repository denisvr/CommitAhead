using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class ReplaceProfileLinksUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_ReplacesProfileLinksAndReturnsSuccess()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new ReplaceProfileLinksUseCase(repository, new FakeCVPresentationRepository(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "https://github.com/example");

        var result = await useCase.ExecuteAsync([link], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.Success, result);
        Assert.Single(profile.ProfileLinks);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_ReturnsNotFound()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceProfileLinksUseCase(repository, new FakeCVPresentationRepository(), new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync([], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.NotFound, result);
    }
}
