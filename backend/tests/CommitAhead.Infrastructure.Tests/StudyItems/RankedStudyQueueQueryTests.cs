using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.StudyItems;

[Collection(PostgresCollection.Name)]
public class RankedStudyQueueQueryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public RankedStudyQueueQueryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static StudyItem CreateItem(
        Guid ownerUserId,
        string title,
        int importance,
        int initialMastery,
        DateTime createdAtUtc,
        StudyItemStatus status = StudyItemStatus.Active)
    {
        var item = new StudyItem(
            Guid.NewGuid(),
            ownerUserId,
            title,
            StudyItemCategory.Theory,
            importance,
            initialMastery,
            tags: [],
            details: new TheoryDetails(
                summaryMarkdown: "Summary",
                keyPoints: ["Point"],
                interviewQuestions: ["Question?"],
                references: []),
            createdAtUtc: createdAtUtc);

        if (status == StudyItemStatus.Archived)
        {
            item.Archive(createdAtUtc);
        }

        return item;
    }

    [Fact]
    public async Task Execute_OrdersByEffectiveScoreDescending()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await repository.AddAsync(CreateItem(ownerUserId, "Low", importance: 1, initialMastery: 5, now), CancellationToken.None);
        await repository.AddAsync(CreateItem(ownerUserId, "High", importance: 5, initialMastery: 1, now), CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(ownerUserId, ScoringWeights.Default, CancellationToken.None);

        Assert.Equal(["High", "Low"], ranked.Select(r => r.Title));
        Assert.True(ranked[0].EffectiveScore > ranked[1].EffectiveScore);
    }

    [Fact]
    public async Task Execute_WithTiedEffectiveScore_BreaksTiesByCreatedAtThenId()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        var earlier = DateTime.UtcNow.AddDays(-1);
        var later = DateTime.UtcNow;

        var olderItem = CreateItem(ownerUserId, "Older", importance: 3, initialMastery: 3, earlier);
        var newerItem = CreateItem(ownerUserId, "Newer", importance: 3, initialMastery: 3, later);
        await repository.AddAsync(olderItem, CancellationToken.None);
        await repository.AddAsync(newerItem, CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(ownerUserId, ScoringWeights.Default, CancellationToken.None);

        Assert.Equal(["Older", "Newer"], ranked.Select(r => r.Title));
    }

    [Fact]
    public async Task Execute_ExcludesArchivedItems()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await repository.AddAsync(CreateItem(ownerUserId, "Archived", importance: 5, initialMastery: 1, now, StudyItemStatus.Archived), CancellationToken.None);
        await repository.AddAsync(CreateItem(ownerUserId, "Active", importance: 1, initialMastery: 5, now), CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(ownerUserId, ScoringWeights.Default, CancellationToken.None);

        Assert.Equal(["Active"], ranked.Select(r => r.Title));
    }

    [Fact]
    public async Task Execute_ScopedToADifferentOwner_ReturnsEmpty()
    {
        var repository = new StudyItemRepository(_dbContext);
        var now = DateTime.UtcNow;
        await repository.AddAsync(CreateItem(Guid.NewGuid(), "Someone else's", importance: 3, initialMastery: 3, now), CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(Guid.NewGuid(), ScoringWeights.Default, CancellationToken.None);

        Assert.Empty(ranked);
    }

    [Fact]
    public async Task Execute_WithPriorityOverride_UsesTheOverrideScoreInstead()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        var item = CreateItem(ownerUserId, "Overridden", importance: 1, initialMastery: 5, DateTime.UtcNow);
        item.SetPriorityOverride(new PriorityOverride(95, "Interview tomorrow"), DateTime.UtcNow);
        await repository.AddAsync(item, CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(ownerUserId, ScoringWeights.Default, CancellationToken.None);

        Assert.Equal(95, ranked[0].EffectiveScore);
        Assert.Equal(95, ranked[0].PriorityOverrideScore);
        Assert.Equal("Interview tomorrow", ranked[0].PriorityOverrideReason);
    }

    [Fact]
    public async Task Execute_IncludesDemandFromEvidenceLinksTargetingTheItem()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        var item = CreateItem(ownerUserId, "Linked", importance: 1, initialMastery: 5, DateTime.UtcNow);
        await repository.AddAsync(item, CancellationToken.None);
        _dbContext.EvidenceLinks.Add(new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), item.Id, 5m, "Mentioned in job posting", DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var query = new RankedStudyQueueQuery(_dbContext);
        var ranked = await query.ExecuteAsync(ownerUserId, ScoringWeights.Default, CancellationToken.None);

        // importance=1, mastery=5, demand=5 -> default weights: 8 (importance) + 35 (demand) + 0 = 43.
        Assert.Equal(5m, ranked[0].Demand);
        Assert.Equal(43, ranked[0].EffectiveScore);
    }
}
