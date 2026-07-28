using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Tests.Identity;

public class FakeUserRepositoryTests
{
    [Fact]
    public async Task AddThenGetBySupabaseUserId_ReturnsTheSameUser()
    {
        var repository = new FakeUserRepository();
        var user = new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", DateTime.UtcNow);

        await repository.AddAsync(user, CancellationToken.None);
        var found = await repository.GetBySupabaseUserIdAsync("supabase-sub-123", CancellationToken.None);

        Assert.Same(user, found);
    }

    [Fact]
    public async Task GetBySupabaseUserId_WhenNotFound_ReturnsNull()
    {
        var repository = new FakeUserRepository();

        var found = await repository.GetBySupabaseUserIdAsync("missing-sub", CancellationToken.None);

        Assert.Null(found);
    }
}
