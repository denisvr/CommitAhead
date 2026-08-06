using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class ReplaceProfileLinkSelectionsUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_SelectingAnExistingLink_ReplacesSelectionsAndReturnsSuccess()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "https://github.com/example");
        profile.ReplaceProfileLinks([link], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var useCase = new ReplaceProfileLinkSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, [link.Id], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal([link.Id], presentation.ProfileLinkSelections);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceProfileLinkSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), [], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }
}
