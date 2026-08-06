using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class ReplaceExperienceSelectionsUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static ExperienceEntry CreateEntry() => new(
        Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], []);

    [Fact]
    public async Task ExecuteAsync_SelectingAnExistingEntry_ReplacesSelectionsAndReturnsSuccess()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        var entry = CreateEntry();
        profile.ReplaceExperience([entry], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var useCase = new ReplaceExperienceSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, [entry.Id], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal([entry.Id], presentation.ExperienceSelections);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceExperienceSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), [], CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_SelectingAnEntryFromAnotherProfile_PropagatesTheDomainValidationException()
    {
        // Invariant 23 (application-enforced per ADR-0012): the entry must belong to the profile
        // this presentation actually references.
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var useCase = new ReplaceExperienceSelectionsUseCase(cvRepository, profileRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(presentation.Id, [Guid.NewGuid()], CancellationToken.None));
    }
}
