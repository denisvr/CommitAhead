using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Identity;

public interface IUserRepository
{
    Task<User?> GetBySupabaseUserIdAsync(string supabaseUserId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
