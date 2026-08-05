using CommitAhead.Domain.Identity;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CommitAhead.Infrastructure.Tests.Security;

/// <summary>
/// Proves the Phase 1 grants/RLS actually work for the real, least-privileged commitahead_app
/// role — not just for whatever superuser Testcontainers hands other test fixtures. Bootstraps a
/// dedicated container the same way backend/scripts/setup-local-db.ps1 bootstraps a real one:
/// 001_roles.sql (as superuser) -> EF migrations (as commitahead_migrator) -> 002/003 RLS scripts
/// (as superuser) -> business queries (as commitahead_app).
/// </summary>
public sealed class RlsIsolationTests : IAsyncLifetime
{
    private const string MigratorPassword = "rls-test-migrator-password";
    private const string AppPassword = "rls-test-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_rls_test")
        .WithUsername("postgres")
        .WithPassword("rls-test-superuser-password")
        .Build();

    private string SuperuserConnectionString => _container.GetConnectionString();

    private string MigratorConnectionString => WithCredentials(_container.GetConnectionString(), "commitahead_migrator", MigratorPassword);

    private string AppConnectionString => WithCredentials(_container.GetConnectionString(), "commitahead_app", AppPassword);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ApplyBootstrapScriptsAsync();

        var migratorOptions = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(MigratorConnectionString).Options;
        await using var migratorDbContext = new CommitAheadDbContext(migratorOptions);
        await migratorDbContext.Database.MigrateAsync();

        await ApplyRlsScriptsAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task ApplyBootstrapScriptsAsync()
    {
        var rolesSql = ReadScript("001_roles.sql")
            .Replace("${COMMITAHEAD_MIGRATOR_PASSWORD}", MigratorPassword)
            .Replace("${COMMITAHEAD_APP_PASSWORD}", AppPassword);

        await ExecuteAsSuperuserAsync(rolesSql);
    }

    private Task ApplyRlsScriptsAsync()
    {
        return ExecuteAsSuperuserAsync(ReadScript("002_rls_users.sql") + "\n" + ReadScript("003_rls_phase1.sql"));
    }

    private async Task ExecuteAsSuperuserAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(SuperuserConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string WithCredentials(string connectionString, string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Username = username, Password = password };
        return builder.ToString();
    }

    private static string ReadScript(string fileName)
    {
        // Tests run from backend/tests/CommitAhead.Infrastructure.Tests/bin/<Config>/<TFM>/.
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "database", fileName));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not locate {fileName} at {path}. Expected backend/scripts/database/ relative to the test output directory.");
        }

        return File.ReadAllText(path);
    }

    private CommitAheadDbContext CreateAppDbContext()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(AppConnectionString).Options;
        return new CommitAheadDbContext(options);
    }

    /// <summary>
    /// commitahead_app now has SELECT-only access to `users` — provisioning is a privileged
    /// operation the application itself can never perform (item 2 of this corrective pass), so
    /// test setup has to mirror that and insert via a privileged connection too, not via
    /// whichever app-role DbContext the test body happens to be using for its own assertions.
    /// </summary>
    private async Task<Guid> CreateUserAsync()
    {
        var id = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(SuperuserConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(id, $"sub-{id}", $"{id}@example.com", DateTime.UtcNow), CancellationToken.None);
        return id;
    }

    private static StudyItem CreateStudyItem(Guid ownerUserId, string title) => new(
        Guid.NewGuid(),
        ownerUserId,
        title,
        StudyItemCategory.Theory,
        importance: 3,
        initialMastery: 3,
        tags: [],
        details: new TheoryDetails("Summary", [], [], []),
        createdAtUtc: DateTime.UtcNow);

    [Fact]
    public async Task Owner_CanCreateReadUpdateAndDeleteTheirOwnStudyItem()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new StudyItemRepository(dbContext);
        var ownerUserId = await CreateUserAsync();
        var item = CreateStudyItem(ownerUserId, "Two Sum");

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            // Create.
            await repository.AddAsync(item, CancellationToken.None);

            // Read.
            var found = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
            Assert.NotNull(found);
            Assert.Equal("Two Sum", found.Title);

            // Update.
            found.Update("Two Sum (revisited)", 5, [], new TheoryDetails("New summary", [], [], []), DateTime.UtcNow);
            await repository.SaveChangesAsync(CancellationToken.None);
            var updated = await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None);
            Assert.Equal("Two Sum (revisited)", updated!.Title);

            // Delete.
            var deleted = await repository.DeleteAsync(updated, CancellationToken.None);
            Assert.True(deleted);
            Assert.Null(await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersStudyItem_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new StudyItemRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var itemA = CreateStudyItem(ownerAId, "Owner A's item");

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(itemA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByIdAsync(ownerAId, itemA.Id, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersStudyItem_EvenWithARawUpdateBypassingTheRepositoryFilter()
    {
        // The repository already scopes GetByIdAsync by owner, so it alone can't prove the
        // database enforces isolation — a raw UPDATE with no owner_user_id filter in its WHERE
        // clause is the real test of RLS's WITH CHECK/USING behaviour, not just app-level scoping.
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new StudyItemRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var itemA = CreateStudyItem(ownerAId, "Owner A's item");
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(itemA, CancellationToken.None), CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE study_items SET title = 'hacked' WHERE id = {itemA.Id}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, async () =>
        {
            var stillIntact = await repository.GetByIdAsync(ownerAId, itemA.Id, CancellationToken.None);
            Assert.Equal("Owner A's item", stillIntact!.Title);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WithoutAnyOwnerContext_QueryingStudyItemsReturnsNoRows_EvenThoughRowsExist()
    {
        await using var setupDbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(setupDbContext);
        var ownerUserId = await CreateUserAsync();
        var repository = new StudyItemRepository(setupDbContext);
        await rlsSessionContext.RunInOwnerScopeAsync(
            ownerUserId, () => repository.AddAsync(CreateStudyItem(ownerUserId, "Should stay invisible"), CancellationToken.None), CancellationToken.None);

        // A fresh DbContext/connection that never calls set_config at all — current_setting(...,
        // true) returns NULL, and owner_user_id = NULL is never true, so this must see zero rows
        // regardless of how many StudyItems actually exist for any owner.
        await using var noContextDbContext = CreateAppDbContext();
        var count = await noContextDbContext.StudyItems.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task TheRuntimeRole_CannotPerformDdl()
    {
        await using var connection = new NpgsqlConnection(AppConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE rls_ddl_probe (id uuid PRIMARY KEY)";

        await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task TheSetupScripts_CorrectAPreviouslyForcedAndOverGrantedState()
    {
        // Simulate a database that already had an EARLIER revision of 002/003 applied — one that
        // forced RLS on every Phase 1 table and granted commitahead_app write access to `users`.
        // Re-running the CURRENT 002/003 must actively correct both, not just layer ENABLE/SELECT
        // on top: ENABLE alone does not clear a previously-set FORCE flag, and GRANT SELECT alone
        // does not take back previously-granted INSERT/UPDATE/DELETE — only the NO FORCE and
        // REVOKE statements those scripts now contain actually undo the old state.
        await ExecuteAsSuperuserAsync(
            """
            ALTER TABLE study_items FORCE ROW LEVEL SECURITY;
            ALTER TABLE study_reviews FORCE ROW LEVEL SECURITY;
            ALTER TABLE scoring_config_overrides FORCE ROW LEVEL SECURITY;
            ALTER TABLE evidence_links FORCE ROW LEVEL SECURITY;
            GRANT INSERT, UPDATE, DELETE ON users TO commitahead_app;
            """);

        await ApplyRlsScriptsAsync();

        // FORCE corrected: commitahead_migrator (the table owner) sees every row without ever
        // setting app.current_user_id — if FORCE were still in effect, the owner would be
        // row-filtered too, exactly like commitahead_app.
        var ownerUserId = await CreateUserAsync();
        await using var appDbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(appDbContext);
        var repository = new StudyItemRepository(appDbContext);
        await rlsSessionContext.RunInOwnerScopeAsync(
            ownerUserId, () => repository.AddAsync(CreateStudyItem(ownerUserId, "Visible to the owner"), CancellationToken.None), CancellationToken.None);

        var migratorOptions = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(MigratorConnectionString).Options;
        await using var migratorDbContext = new CommitAheadDbContext(migratorOptions);
        Assert.Equal(1, await migratorDbContext.StudyItems.CountAsync());

        // Grant corrected: commitahead_app can no longer write to `users`, only read it.
        var appOptions = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(AppConnectionString).Options;
        await using var appOnlyDbContext = new CommitAheadDbContext(appOptions);
        var probe = new User(Guid.NewGuid(), "revoke-probe", "revoke-probe@example.com", DateTime.UtcNow);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => new UserRepository(appOnlyDbContext).AddAsync(probe, CancellationToken.None));
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, postgresException.SqlState);
    }

    [Fact]
    public async Task TheSetupScripts_RemainSafe_WhenAppliedASecondTime()
    {
        // setup-local-db.ps1 re-applies 001/002/003 on every invocation, not just against a fresh
        // volume (see its own header comment) — this proves that's actually safe, beyond the
        // single application InitializeAsync already performed for every other test here.
        await ApplyBootstrapScriptsAsync();
        await ApplyRlsScriptsAsync();

        // The database must still work normally afterward.
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var ownerUserId = await CreateUserAsync();
        var repository = new StudyItemRepository(dbContext);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            var item = CreateStudyItem(ownerUserId, "Still works");
            await repository.AddAsync(item, CancellationToken.None);
            Assert.NotNull(await repository.GetByIdAsync(ownerUserId, item.Id, CancellationToken.None));
        }, CancellationToken.None);
    }
}
