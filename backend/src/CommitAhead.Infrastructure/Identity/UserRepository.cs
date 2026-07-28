using CommitAhead.Application.Identity;
using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Identity;

public sealed class UserRepository : IUserRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public UserRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetBySupabaseUserIdAsync(string supabaseUserId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .SingleOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
