using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommitAhead.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef migrations add/update` at design time. `dotnet ef migrations add`
/// never connects, so the fallback dummy connection string is enough for it. `dotnet ef
/// database update` does connect — point it at a real migration-role connection via the
/// COMMITAHEAD_MIGRATION_CONNECTION environment variable (never committed). The running API
/// never uses this factory; it reads ConnectionStrings:CommitAheadDb from app configuration.
/// </summary>
public sealed class CommitAheadDbContextFactory : IDesignTimeDbContextFactory<CommitAheadDbContext>
{
    public CommitAheadDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMMITAHEAD_MIGRATION_CONNECTION")
            ?? "Host=localhost;Database=commitahead_designtime;Username=designtime;Password=designtime";

        var optionsBuilder = new DbContextOptionsBuilder<CommitAheadDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CommitAheadDbContext(optionsBuilder.Options);
    }
}
