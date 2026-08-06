using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class CreateCVPresentationUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithTheCurrentUsersOwnProfile_CreatesAPresentation()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "https://github.com/example");
        profile.ReplaceProfileLinks([link], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var useCase = new CreateCVPresentationUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(profile.Id, "UK — Senior Backend Engineer", "United Kingdom", null, "en-GB", "modern-one-page", null, false, true, true, false, "dd MMM yyyy", 2, CancellationToken.None);

        Assert.NotNull(id);
        var created = Assert.Single(cvRepository.Presentations);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultsProfileLinkSelectionsToEveryExistingLink()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "https://github.com/example");
        profile.ReplaceProfileLinks([link], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var useCase = new CreateCVPresentationUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, CancellationToken.None);

        var created = Assert.Single(cvRepository.Presentations);
        Assert.Equal([link.Id], created.ProfileLinkSelections);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMatchingProfile_ReturnsNull()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var useCase = new CreateCVPresentationUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, CancellationToken.None);

        Assert.Null(id);
        Assert.Empty(cvRepository.Presentations);
    }

    [Fact]
    public async Task ExecuteAsync_ReferencingAnotherOwnersProfile_ReturnsNull()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var otherOwnersProfile = new ProfessionalProfile(Guid.NewGuid(), Guid.NewGuid(), ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await profileRepository.AddAsync(otherOwnersProfile, CancellationToken.None);
        var useCase = new CreateCVPresentationUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(otherOwnersProfile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, CancellationToken.None);

        Assert.Null(id);
    }
}
