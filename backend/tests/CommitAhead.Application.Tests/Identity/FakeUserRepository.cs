using CommitAhead.Application.Identity;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Tests.Identity;

/// <summary>
/// Handwritten in-memory fake for use-case tests, per docs/testing/strategy.md Layer 2.
/// </summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<User?> GetBySupabaseUserIdAsync(string supabaseUserId, CancellationToken cancellationToken)
    {
        var user = _users.SingleOrDefault(u => u.SupabaseUserId == supabaseUserId);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}
