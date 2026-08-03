using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;

namespace CommitAhead.Infrastructure.Tests.Persistence;

/// <summary>
/// Every user-owned Phase 1 table now has a real FK to users.id — tests that create StudyItems,
/// EvidenceLinks, or ScoringConfigOverrides must create a real User row first, not just a random
/// Guid, or the insert fails with a foreign-key violation.
/// </summary>
internal static class TestUsers
{
    public static async Task<Guid> CreateAsync(CommitAheadDbContext dbContext)
    {
        var id = Guid.NewGuid();
        var user = new User(id, $"sub-{id}", $"{id}@example.com", DateTime.UtcNow);
        await new UserRepository(dbContext).AddAsync(user, CancellationToken.None);
        return id;
    }
}
