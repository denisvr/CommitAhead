using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Identity;

public interface IUserRepository
{
    Task<User?> GetBySupabaseUserIdAsync(string supabaseUserId, CancellationToken cancellationToken);

    /// <summary>Looks up a user by email. The caller must pass an already-normalized email (see <see cref="User.Normalize"/>).</summary>
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
