using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.StudyItems;

[Collection(PostgresCollection.Name)]
public class StudyItemRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public StudyItemRepositoryTests(PostgresContainerFixture fixture)
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

    private static StudyItem CreateItem(Guid ownerUserId, PriorityOverride? priorityOverride = null)
    {
        var item = new StudyItem(
            Guid.NewGuid(),
            ownerUserId,
            "Two Sum",
            StudyItemCategory.LeetCode,
            importance: 4,
            initialMastery: 2,
            tags: ["Arrays", "Hash Table"],
            details: new LeetCodeDetails(
                problemNumber: 1,
                url: "https://leetcode.com/problems/two-sum",
                difficulty: Difficulty.Easy,
                patterns: ["Two Pointers"],
                expectedTimeComplexity: "O(n)",
                expectedSpaceComplexity: "O(n)",
                approachMarkdown: "Use a hash map.",
                csharpSolution: null),
            createdAtUtc: DateTime.UtcNow);

        if (priorityOverride is not null)
        {
            item.SetPriorityOverride(priorityOverride, DateTime.UtcNow);
        }

        return item;
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsTheItem_IncludingDetailsAndTags()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId);

        await repository.AddAsync(item, CancellationToken.None);
        var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(item.Title, found.Title);
        Assert.Equal(["arrays", "hash-table"], found.Tags);
        var details = Assert.IsType<LeetCodeDetails>(found.Details);
        Assert.Equal(1, details.ProblemNumber);
        Assert.Equal(["two-pointers"], details.Patterns);
    }

    [Fact]
    public async Task GetById_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new StudyItemRepository(_dbContext);
        var item = CreateItem(await TestUsers.CreateAsync(_dbContext));
        await repository.AddAsync(item, CancellationToken.None);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), item.Id, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task AddThenGetById_WithPriorityOverride_RoundTripsIt()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId, new PriorityOverride(90, "Interview next week"));

        await repository.AddAsync(item, CancellationToken.None);
        var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);

        Assert.NotNull(found?.PriorityOverride);
        Assert.Equal(90, found.PriorityOverride.Score);
        Assert.Equal("Interview next week", found.PriorityOverride.Reason);
    }

    [Fact]
    public async Task AddThenGetById_WithoutPriorityOverride_RoundTripsAsNull()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId);

        await repository.AddAsync(item, CancellationToken.None);
        var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);

        Assert.Null(found?.PriorityOverride);
    }

    [Fact]
    public async Task AddThenAddReviewThenGetById_RoundTripsTheReview()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId);
        await repository.AddAsync(item, CancellationToken.None);

        var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
        found!.AddReview(new StudyReview(Guid.NewGuid(), DateTime.UtcNow, 4, "Went well"), DateTime.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);

        Assert.Single(reloaded!.Reviews);
        Assert.Equal(4, reloaded.Reviews[0].ConfidenceRating);
        Assert.Equal("Went well", reloaded.Reviews[0].NotesMarkdown);
    }

    [Fact]
    public async Task Delete_WithNoReviews_RemovesTheItem()
    {
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId);
        await repository.AddAsync(item, CancellationToken.None);

        var deleted = await repository.DeleteAsync(item, CancellationToken.None);

        Assert.True(deleted);
        var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
        Assert.Null(found);
    }

    [Fact]
    public async Task Delete_WithAConcurrentlyInsertedReview_IsRejectedByTheDatabase()
    {
        // The database is the final protection (model.md invariant 2) for the race
        // DeleteStudyItemUseCase's own CanBeHardDeleted guard cannot close by itself: a review
        // inserted through a SEPARATE DbContext (a different request) after this repository's own
        // `item` was loaded/created. `item`'s own tracked Reviews collection is still empty, so
        // only the database's Restrict FK — not EF's client-side cascade check, which only sees
        // what THIS context already has loaded — can catch it.
        var repository = new StudyItemRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var item = CreateItem(ownerUserId);
        await repository.AddAsync(item, CancellationToken.None);

        var otherOptions = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        await using var otherDbContext = new CommitAheadDbContext(otherOptions);
        var otherRepository = new StudyItemRepository(otherDbContext);
        var trackedElsewhere = await otherRepository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
        trackedElsewhere!.AddReview(new StudyReview(Guid.NewGuid(), DateTime.UtcNow, 4, null), DateTime.UtcNow);
        await otherRepository.SaveChangesAsync(CancellationToken.None);

        var deleted = await repository.DeleteAsync(item, CancellationToken.None);

        Assert.False(deleted);
        var stillThere = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
        Assert.NotNull(stillThere);
        Assert.Single(stillThere.Reviews);
    }
}
