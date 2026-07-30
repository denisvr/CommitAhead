using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.StudyItems;

[Collection(PostgresCollection.Name)]
public class ScoringConfigRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public ScoringConfigRepositoryTests(PostgresContainerFixture fixture)
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

    [Fact]
    public async Task GetOverride_WhenNoneStored_ReturnsNull()
    {
        var repository = new ScoringConfigRepository(_dbContext);

        var weights = await repository.GetOverrideAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(weights);
    }

    [Fact]
    public async Task SetThenGetOverride_RoundTripsTheWeights()
    {
        var repository = new ScoringConfigRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();

        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);
        var found = await repository.GetOverrideAsync(ownerUserId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(50, found.ImportanceWeight);
        Assert.Equal(30, found.DemandWeight);
        Assert.Equal(20, found.MasteryGapWeight);
    }

    [Fact]
    public async Task SetOverrideTwice_UpdatesTheExistingRowRatherThanDuplicating()
    {
        var repository = new ScoringConfigRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();

        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);
        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(60, 20, 20), CancellationToken.None);
        var found = await repository.GetOverrideAsync(ownerUserId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(60, found.ImportanceWeight);
    }

    [Fact]
    public async Task Reset_RemovesTheOverride()
    {
        var repository = new ScoringConfigRepository(_dbContext);
        var ownerUserId = Guid.NewGuid();
        await repository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);

        await repository.ResetAsync(ownerUserId, CancellationToken.None);
        var found = await repository.GetOverrideAsync(ownerUserId, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Reset_WhenNoneStored_DoesNotThrow()
    {
        var repository = new ScoringConfigRepository(_dbContext);

        await repository.ResetAsync(Guid.NewGuid(), CancellationToken.None);
    }
}
