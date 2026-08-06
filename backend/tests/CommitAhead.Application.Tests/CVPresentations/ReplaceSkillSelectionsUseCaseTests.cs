using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class ReplaceSkillSelectionsUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_SelectingAnExistingSkill_ReplacesSelectionsAndReturnsSuccess()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var skill = new Skill(Guid.NewGuid(), "C#", SkillCategory.Language, null);
        profile.ReplaceSkills([skill], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var useCase = new ReplaceSkillSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, [skill.Id], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal([skill.Id], presentation.SkillSelections);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceSkillSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), [], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }
}
