using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class ReplaceProjectSelectionsUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_SelectingAnExistingEntry_ReplacesSelectionsAndReturnsSuccess()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var entry = new ProjectEntry(Guid.NewGuid(), "CommitAhead", null, null, null, "An interview-prep app.", null, []);
        profile.ReplaceProjects([entry], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var useCase = new ReplaceProjectSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, [entry.Id], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal([entry.Id], presentation.ProjectSelections);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceProjectSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), [], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }
}
