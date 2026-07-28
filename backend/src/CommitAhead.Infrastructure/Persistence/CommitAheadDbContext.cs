using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Persistence;

public sealed class CommitAheadDbContext : DbContext
{
    public CommitAheadDbContext(DbContextOptions<CommitAheadDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
