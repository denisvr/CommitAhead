using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.Identity;

[Collection(PostgresCollection.Name)]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public UserRepositoryTests(PostgresContainerFixture fixture)
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
    public async Task AddThenGetBySupabaseUserId_RoundTripsTheUser()
    {
        var repository = new UserRepository(_dbContext);
        var user = new User(Guid.NewGuid(), "supabase-sub-abc", "owner@example.com", DateTime.UtcNow);

        await repository.AddAsync(user, CancellationToken.None);
        var found = await repository.GetBySupabaseUserIdAsync("supabase-sub-abc", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        Assert.Equal(user.Email, found.Email);
        Assert.True(found.IsEnabled);
    }

    [Fact]
    public async Task GetBySupabaseUserId_WhenNotFound_ReturnsNull()
    {
        var repository = new UserRepository(_dbContext);

        var found = await repository.GetBySupabaseUserIdAsync("does-not-exist", CancellationToken.None);

        Assert.Null(found);
    }
}
