using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class ReplaceProjectsUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_ReplacesProjectsAndReturnsSuccess()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new ReplaceProjectsUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });
        var entry = new ProjectEntry(Guid.NewGuid(), "CommitAhead", null, null, null, "An interview-prep app.", null, []);

        var result = await useCase.ExecuteAsync([entry], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.Success, result);
        Assert.Single(profile.Projects);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_ReturnsNotFound()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceProjectsUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync([], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.NotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_ReferencingANonexistentSkill_PropagatesTheDomainValidationException()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow), CancellationToken.None);
        var useCase = new ReplaceProjectsUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });
        var entry = new ProjectEntry(Guid.NewGuid(), "CommitAhead", null, null, null, "An interview-prep app.", null, [Guid.NewGuid()]);

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync([entry], CancellationToken.None));
    }
}
