using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

public class GetProfessionalProfileUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    [Fact]
    public async Task ExecuteAsync_WithNoProfileYet_ReturnsNull()
    {
        var repository = new FakeProfessionalProfileRepository();
        var useCase = new GetProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnExistingProfile_ReturnsItsProjection()
    {
        var repository = new FakeProfessionalProfileRepository();
        var ownerUserId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Backend engineer.", DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);
        var useCase = new GetProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal("Backend engineer.", result.SummaryMarkdown);
    }

    [Fact]
    public async Task ExecuteAsync_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new FakeProfessionalProfileRepository();
        await repository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), Guid.NewGuid(), ValidContactInfo(), "Someone else's summary.", DateTime.UtcNow), CancellationToken.None);
        var useCase = new GetProfessionalProfileUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Null(result);
    }
}
