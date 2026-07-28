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

    [Fact]
    public async Task AddThenGetByNormalizedEmail_RoundTripsTheUser()
    {
        var repository = new UserRepository(_dbContext);
        var user = new User(Guid.NewGuid(), "supabase-sub-email", "Owner@Example.com", DateTime.UtcNow);

        await repository.AddAsync(user, CancellationToken.None);
        var found = await repository.GetByNormalizedEmailAsync("owner@example.com", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
    }

    [Fact]
    public async Task GetByNormalizedEmail_WhenNotFound_ReturnsNull()
    {
        var repository = new UserRepository(_dbContext);

        var found = await repository.GetByNormalizedEmailAsync("does-not-exist@example.com", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task AddingASecondUser_WithTheSameEmailDifferentCase_ViolatesTheUniqueIndex()
    {
        var repository = new UserRepository(_dbContext);
        await repository.AddAsync(new User(Guid.NewGuid(), "supabase-sub-dup-1", "dup@example.com", DateTime.UtcNow), CancellationToken.None);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            repository.AddAsync(new User(Guid.NewGuid(), "supabase-sub-dup-2", "DUP@Example.com", DateTime.UtcNow), CancellationToken.None));
    }
}
