using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace CommitAhead.Infrastructure.Tests.Persistence;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_test")
        .WithUsername("commitahead_test")
        .WithPassword("commitahead_test")
        .Build();

    private NpgsqlConnection _respawnConnection = null!;
    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<CommitAheadDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var dbContext = new CommitAheadDbContext(options);
        await dbContext.Database.MigrateAsync();

        _respawnConnection = new NpgsqlConnection(ConnectionString);
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
        });
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_respawnConnection);
    }

    public async Task DisposeAsync()
    {
        await _respawnConnection.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
