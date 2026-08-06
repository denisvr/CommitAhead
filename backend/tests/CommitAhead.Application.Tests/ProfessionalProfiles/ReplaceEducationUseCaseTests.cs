using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class ReplaceEducationUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_ReplacesEducationAndReturnsSuccess()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new ReplaceEducationUseCase(repository, new FakeCVPresentationRepository(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });
        var entry = new EducationEntry(Guid.NewGuid(), "MIT", "BSc", null, null, null, null, null);

        var result = await useCase.ExecuteAsync([entry], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.Success, result);
        Assert.Single(profile.Education);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_ReturnsNotFound()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new ReplaceEducationUseCase(repository, new FakeCVPresentationRepository(), new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync([], CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.NotFound, result);
    }
}
