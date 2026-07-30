using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class GetStudyItemUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WhenItemDoesNotExist_ReturnsNull()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new GetStudyItemUseCase(repository, new FakeScoringConfigRepository(), new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoReviews_UsesInitialMastery_AndComputesEffectiveScore()
    {
        var ownerUserId = Guid.NewGuid();
        // importance=1, initialMastery=5 -> mastery=5, demand=0 -> default weights give score 8 (ADR-0003).
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 1, 5, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new GetStudyItemUseCase(repository, new FakeScoringConfigRepository(), new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5m, result!.Mastery);
        Assert.Equal(0m, result.Demand);
        Assert.Equal(8, result.EffectiveScore);
        Assert.Equal(8, result.ScoreBreakdown.ImportanceContribution);
        Assert.Equal(0m, result.ScoreBreakdown.DemandContribution);
        Assert.Equal(0m, result.ScoreBreakdown.MasteryGapContribution);
        Assert.Equal(8, result.ScoreBreakdown.Total);
        Assert.Empty(result.Reviews);
    }

    [Fact]
    public async Task ExecuteAsync_WithAPriorityOverride_ReturnsTheOverrideScore()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 5, 1, [], new TheoryDetails("s", [], [], []), Now);
        item.SetPriorityOverride(new PriorityOverride(0, "Deprioritised"), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new GetStudyItemUseCase(repository, new FakeScoringConfigRepository(), new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(0, result!.EffectiveScore);
        Assert.Equal(0, result.PriorityOverrideScore);
        Assert.Equal("Deprioritised", result.PriorityOverrideReason);
    }

    [Fact]
    public async Task ExecuteAsync_WithDemandFromEvidenceLinks_IncludesItInTheEffectiveScoreAndBreakdown()
    {
        var ownerUserId = Guid.NewGuid();
        // importance=1, mastery=5 (no reviews), demand=5 -> default weights: 8 (importance) + 35 (demand) + 0 = 43.
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 1, 5, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new GetStudyItemUseCase(repository, new FakeScoringConfigRepository(), new FakeEvidenceLinkQuery { Demand = 5m }, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(5m, result!.Demand);
        Assert.Equal(43, result.EffectiveScore);
        Assert.Equal(35m, result.ScoreBreakdown.DemandContribution);
    }
}
