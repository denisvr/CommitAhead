using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class CreateProfessionalProfileUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithNoExistingProfile_CreatesOneOwnedByTheCurrentUser()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var useCase = new CreateProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(ValidContactInfo(), "Backend engineer.", CancellationToken.None);

        Assert.NotNull(id);
        var created = Assert.Single(repository.Profiles);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheCurrentUserAlreadyHasAProfile_ReturnsNull()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Existing summary.", DateTime.UtcNow), CancellationToken.None);
        var useCase = new CreateProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(ValidContactInfo(), "New summary.", CancellationToken.None);

        Assert.Null(id);
        Assert.Single(repository.Profiles);
    }
}
