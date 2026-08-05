using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class ScoringConfigUseCaseTests
{
    [Fact]
    public async Task UpdateScoringConfig_PersistsTheGivenWeights()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeScoringConfigRepository();
        var useCase = new UpdateScoringConfigUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(50, 30, 20, CancellationToken.None);

        var stored = await repository.GetOverrideAsync(ownerUserId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(50, stored!.ImportanceWeight);
    }

    [Fact]
    public async Task UpdateScoringConfig_WithWeightsNotSummingTo100_Throws()
    {
        var repository = new FakeScoringConfigRepository();
        var useCase = new UpdateScoringConfigUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(50, 30, 30, CancellationToken.None));
    }

    [Fact]
    public async Task ResetScoringConfig_RemovesAnExistingOverride()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeScoringConfigRepository();
        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);
        var useCase = new ResetScoringConfigUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Null(await repository.GetOverrideAsync(ownerUserId, CancellationToken.None));
    }

    [Fact]
    public async Task GetScoringConfig_WithNoOverride_ReturnsDefaultsAndIsOverriddenFalse()
    {
        var repository = new FakeScoringConfigRepository();
        var useCase = new GetScoringConfigUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(ScoringWeights.Default.ImportanceWeight, result.ImportanceWeight);
        Assert.Equal(ScoringWeights.Default.DemandWeight, result.DemandWeight);
        Assert.Equal(ScoringWeights.Default.MasteryGapWeight, result.MasteryGapWeight);
        Assert.False(result.IsOverridden);
    }

    [Fact]
    public async Task GetScoringConfig_WithAnOverride_ReturnsItAndIsOverriddenTrue()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeScoringConfigRepository();
        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);
        var useCase = new GetScoringConfigUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(50, result.ImportanceWeight);
        Assert.True(result.IsOverridden);
    }
}
