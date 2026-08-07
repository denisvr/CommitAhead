using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.Identity;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CommitAhead.Infrastructure.Tests.Security;

/// <summary>
/// The Phase 4 counterpart to RlsIsolationPhase3Tests — proves 007_rls_phase4.sql's grants/RLS
/// actually work for the real, least-privileged commitahead_app role against analysis_drafts, one
/// representative transitively-scoped child table (link_proposals), and ai_usage_records.
/// Bootstraps a dedicated container the same way backend/scripts/setup-local-db.ps1 does:
/// 001_roles.sql (as superuser) -> EF migrations (as commitahead_migrator) -> 002-005/007 RLS
/// scripts (as superuser) -> business queries (as commitahead_app).
/// </summary>
public sealed class RlsIsolationPhase4Tests : IAsyncLifetime
{
    private const string MigratorPassword = "rls-phase4-migrator-password";
    private const string AppPassword = "rls-phase4-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_rls_phase4_test")
        .WithUsername("postgres")
        .WithPassword("rls-phase4-superuser-password")
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
        return ExecuteAsSuperuserAsync(
            ReadScript("002_rls_users.sql") + "\n" + ReadScript("003_rls_phase1.sql") + "\n" + ReadScript("004_rls_phase2.sql") + "\n"
                + ReadScript("005_rls_phase3.sql") + "\n" + ReadScript("007_rls_phase4.sql"));
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

    /// <summary>commitahead_app has SELECT-only access to `users` — test setup inserts via a privileged connection, same as RlsIsolationPhase3Tests.</summary>
    private async Task<Guid> CreateUserAsync()
    {
        var id = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(SuperuserConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(id, $"sub-{id}", $"{id}@example.com", DateTime.UtcNow), CancellationToken.None);
        return id;
    }

    private static AnalysisDraft CreateDraft(Guid ownerUserId)
    {
        var link = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");
        return new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [link], [], DateTime.UtcNow);
    }

    private static AIUsageRecord CreateUsageRecord(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, $"key-{Guid.NewGuid():N}", AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
        "anthropic", "claude-fake", "2026-01-01", "usd", 1000, 500, 0.05m, DateTime.UtcNow);

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersAnalysisDraft_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new AnalysisDraftRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var draftA = CreateDraft(ownerAId);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(draftA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByIdAsync(ownerAId, draftA.Id, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersAnalysisDraft_EvenWithARawUpdateBypassingTheRepositoryFilter()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new AnalysisDraftRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var draftA = CreateDraft(ownerAId);
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(draftA, CancellationToken.None), CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE analysis_drafts SET status = 'Discarded' WHERE id = {draftA.Id}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    /// <summary>link_proposals has no owner_user_id column — isolation is transitive through analysis_drafts, exactly like job_requirements in the Phase 3 test.</summary>
    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersLinkProposal_TransitivelyThroughItsParentDraft()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new AnalysisDraftRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var draftA = CreateDraft(ownerAId);
        var linkProposalId = draftA.LinkProposals[0].Id;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(draftA, CancellationToken.None), CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE link_proposals SET proposed_rationale = 'hacked' WHERE id = {linkProposalId}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersAIUsageRecord_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var repository = new AIUsageRecordRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var recordA = CreateUsageRecord(ownerAId);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(recordA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByIdempotencyKeyAsync(ownerAId, recordA.IdempotencyKey, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WithoutAnyOwnerContext_QueryingAnalysisDraftsAndAIUsageRecordsReturnsNoRows_EvenThoughRowsExist()
    {
        await using var setupDbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(setupDbContext);
        var draftRepository = new AnalysisDraftRepository(setupDbContext);
        var usageRepository = new AIUsageRecordRepository(setupDbContext);
        var ownerUserId = await CreateUserAsync();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            await draftRepository.AddAsync(CreateDraft(ownerUserId), CancellationToken.None);
            await usageRepository.AddAsync(CreateUsageRecord(ownerUserId), CancellationToken.None);
        }, CancellationToken.None);

        // A fresh DbContext/connection that never calls set_config at all — current_setting(...,
        // true) returns NULL, and owner_user_id = NULL is never true, so both queries must see
        // zero rows regardless of how many rows actually exist for any owner.
        await using var noContextDbContext = CreateAppDbContext();
        Assert.Equal(0, await noContextDbContext.AnalysisDrafts.CountAsync());
        Assert.Equal(0, await noContextDbContext.AIUsageRecords.CountAsync());
    }

    [Fact]
    public async Task TheSetupScripts_RemainSafe_WhenAppliedASecondTime()
    {
        // setup-local-db.ps1 re-applies 001-005/007 on every invocation, not just against a fresh
        // volume — this proves 007_rls_phase4.sql specifically is safe to run twice, beyond the
        // single application InitializeAsync already performed for every other test here.
        await ApplyBootstrapScriptsAsync();
        await ApplyRlsScriptsAsync();

        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext);
        var draftRepository = new AnalysisDraftRepository(dbContext);
        var usageRepository = new AIUsageRecordRepository(dbContext);
        var ownerUserId = await CreateUserAsync();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            var draft = CreateDraft(ownerUserId);
            await draftRepository.AddAsync(draft, CancellationToken.None);
            var record = CreateUsageRecord(ownerUserId);
            await usageRepository.AddAsync(record, CancellationToken.None);

            Assert.NotNull(await draftRepository.GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None));
            Assert.NotNull(await usageRepository.GetByIdempotencyKeyAsync(ownerUserId, record.IdempotencyKey, CancellationToken.None));
        }, CancellationToken.None);
    }
}
