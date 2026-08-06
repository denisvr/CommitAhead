using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class ReplaceExperienceUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static ExperienceEntry CreateEntry(IReadOnlyList<Guid>? skillIds = null) => new(
        Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], skillIds ?? []);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_ReplacesExperienceAndReturnsSuccess()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new ReplaceExperienceUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync([CreateEntry()], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.Success, result);
        Assert.Single(profile.Experience);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_ReturnsNotFound()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceExperienceUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync([CreateEntry()], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.NotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_ReferencingANonexistentSkill_PropagatesTheDomainValidationException()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow), CancellationToken.None);
        var useCase = new ReplaceExperienceUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync([CreateEntry([Guid.NewGuid()])], CancellationToken.None));
    }
}
