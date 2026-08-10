using CommitAhead.Domain.Identity;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.InterviewNotes;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CommitAhead.Infrastructure.Tests.Security;

/// <summary>
/// The Phase 3 counterpart to RlsIsolationTests/RlsIsolationPhase2Tests — proves
/// 005_rls_phase3.sql's grants/RLS actually work for the real, least-privileged commitahead_app
/// role against job_analyses, one representative transitively-scoped child table
/// (job_requirements), and interview_notes. Bootstraps a dedicated container the same way
/// backend/scripts/setup-local-db.ps1 does: 001_roles.sql (as superuser) -> EF migrations (as
/// commitahead_migrator) -> 002/003/004/005 RLS scripts (as superuser) -> business queries (as
/// commitahead_app).
/// </summary>
public sealed class RlsIsolationPhase3Tests : IAsyncLifetime
{
    private const string MigratorPassword = "rls-phase3-migrator-password";
    private const string AppPassword = "rls-phase3-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_rls_phase3_test")
        .WithUsername("postgres")
        .WithPassword("rls-phase3-superuser-password")
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
            ReadScript("002_rls_users.sql") + "\n" + ReadScript("003_rls_phase1.sql") + "\n" + ReadScript("004_rls_phase2.sql") + "\n" + ReadScript("005_rls_phase3.sql"));
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

    /// <summary>commitahead_app has SELECT-only access to `users` — test setup inserts via a privileged connection, same as RlsIsolationTests.</summary>
    private async Task<Guid> CreateUserAsync()
    {
        var id = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(SuperuserConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(id, $"sub-{id}", $"{id}@example.com", DateTime.UtcNow), CancellationToken.None);
        return id;
    }

    private static JobAnalysis CreateAnalysis(Guid ownerUserId, string title) => new(
        Guid.NewGuid(), ownerUserId, title, new PastedText("Job posting text."), null, DateTime.UtcNow);

    private static InterviewNote CreateNote(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], null, DateTime.UtcNow);

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersJobAnalysis_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new JobAnalysisRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var analysisA = CreateAnalysis(ownerAId, "Owner A's analysis");

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(analysisA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByIdAsync(ownerAId, analysisA.Id, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersJobAnalysis_EvenWithARawUpdateBypassingTheRepositoryFilter()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new JobAnalysisRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var analysisA = CreateAnalysis(ownerAId, "Owner A's analysis");
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(analysisA, CancellationToken.None), CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE job_analyses SET title = 'hacked' WHERE id = {analysisA.Id}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    /// <summary>job_requirements has no owner_user_id column — isolation is transitive through job_analyses, exactly like skills in the Phase 2 test.</summary>
    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersJobRequirement_TransitivelyThroughItsParentAnalysis()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new JobAnalysisRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var analysisA = CreateAnalysis(ownerAId, "Owner A's analysis");
        var requirementId = Guid.NewGuid();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, async () =>
        {
            await repository.AddAsync(analysisA, CancellationToken.None);
            analysisA.AddRequirement(
                new JobRequirement(requirementId, "5+ years of C#.", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience."),
                DateTime.UtcNow);
            await repository.SaveChangesAsync(CancellationToken.None);
        }, CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE job_requirements SET text = 'hacked' WHERE id = {requirementId}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersInterviewNote_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new InterviewNoteRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var noteA = CreateNote(ownerAId);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(noteA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByIdAsync(ownerAId, noteA.Id, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WithoutAnyOwnerContext_QueryingJobAnalysesAndInterviewNotesReturnsNoRows_EvenThoughRowsExist()
    {
        await using var setupDbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(setupDbContext, NullLogger<RlsSessionContext>.Instance);
        var analysisRepository = new JobAnalysisRepository(setupDbContext);
        var noteRepository = new InterviewNoteRepository(setupDbContext);
        var ownerUserId = await CreateUserAsync();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            await analysisRepository.AddAsync(CreateAnalysis(ownerUserId, "Owner's analysis"), CancellationToken.None);
            await noteRepository.AddAsync(CreateNote(ownerUserId), CancellationToken.None);
        }, CancellationToken.None);

        // A fresh DbContext/connection that never calls set_config at all — current_setting(...,
        // true) returns NULL, and owner_user_id = NULL is never true, so both queries must see
        // zero rows regardless of how many rows actually exist for any owner.
        await using var noContextDbContext = CreateAppDbContext();
        Assert.Equal(0, await noContextDbContext.JobAnalyses.CountAsync());
        Assert.Equal(0, await noContextDbContext.InterviewNotes.CountAsync());
    }

    [Fact]
    public async Task TheSetupScripts_RemainSafe_WhenAppliedASecondTime()
    {
        // setup-local-db.ps1 re-applies 001/002/003/004/005 on every invocation, not just against a
        // fresh volume — this proves 005_rls_phase3.sql specifically is safe to run twice, beyond
        // the single application InitializeAsync already performed for every other test here.
        await ApplyBootstrapScriptsAsync();
        await ApplyRlsScriptsAsync();

        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var analysisRepository = new JobAnalysisRepository(dbContext);
        var noteRepository = new InterviewNoteRepository(dbContext);
        var ownerUserId = await CreateUserAsync();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            var analysis = CreateAnalysis(ownerUserId, "Still works");
            await analysisRepository.AddAsync(analysis, CancellationToken.None);
            var note = CreateNote(ownerUserId);
            await noteRepository.AddAsync(note, CancellationToken.None);

            Assert.NotNull(await analysisRepository.GetByIdAsync(ownerUserId, analysis.Id, CancellationToken.None));
            Assert.NotNull(await noteRepository.GetByIdAsync(ownerUserId, note.Id, CancellationToken.None));
        }, CancellationToken.None);
    }
}
