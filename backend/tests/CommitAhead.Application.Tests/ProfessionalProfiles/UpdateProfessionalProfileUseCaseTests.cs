using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class UpdateProfessionalProfileUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_UpdatesItAndReturnsSuccess()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Old summary.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new UpdateProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });
        var newContactInfo = new ContactInfo("Grace Hopper", "grace@example.com", null, null, null);

        var result = await useCase.ExecuteAsync(newContactInfo, "New summary.", CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.Success, result);
        Assert.Equal("New summary.", profile.SummaryMarkdown);
        Assert.Same(newContactInfo, profile.ContactInfo);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_ReturnsNotFound()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new UpdateProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(ValidContactInfo(), "Summary.", CancellationToken.None);

        Assert.Equal(ProfessionalProfileMutationResult.NotFound, result);
    }
}
