using CommitAhead.Application.Identity;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Replaces the real UserRepository (which needs a live Postgres connection) for API tests that
/// only need to exercise the ADR-0015 enabled-user check, per docs/testing/strategy.md's
/// "handwritten repository fakes" pattern.
/// </summary>
public sealed class StubUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _usersBySupabaseId = [];

    public void Add(User user)
    {
        _usersBySupabaseId[user.SupabaseUserId] = user;
    }

    public void Clear()
    {
        _usersBySupabaseId.Clear();
    }

    public Task<User?> GetBySupabaseUserIdAsync(string supabaseUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_usersBySupabaseId.GetValueOrDefault(supabaseUserId));
    }

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return Task.FromResult(_usersBySupabaseId.Values.SingleOrDefault(u => u.Email == normalizedEmail));
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Add(user);
        return Task.CompletedTask;
    }
}
