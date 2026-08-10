using CommitAhead.Domain.Identity;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CommitAhead.Infrastructure.Tests.Security;

/// <summary>
/// The Phase 2 counterpart to RlsIsolationTests — proves 004_rls_phase2.sql's grants/RLS actually
/// work for the real, least-privileged commitahead_app role against professional_profiles, one
/// representative transitively-scoped child table (skills), and cv_presentations. Bootstraps a
/// dedicated container the same way backend/scripts/setup-local-db.ps1 does: 001_roles.sql (as
/// superuser) -> EF migrations (as commitahead_migrator) -> 002/003/004 RLS scripts (as
/// superuser) -> business queries (as commitahead_app).
/// </summary>
public sealed class RlsIsolationPhase2Tests : IAsyncLifetime
{
    private const string MigratorPassword = "rls-phase2-migrator-password";
    private const string AppPassword = "rls-phase2-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_rls_phase2_test")
        .WithUsername("postgres")
        .WithPassword("rls-phase2-superuser-password")
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
        return ExecuteAsSuperuserAsync(ReadScript("002_rls_users.sql") + "\n" + ReadScript("003_rls_phase1.sql") + "\n" + ReadScript("004_rls_phase2.sql"));
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

    private static ContactInfo ValidContactInfo(string name) => new(name, $"{name.ToLowerInvariant().Replace(' ', '.')}@example.com", null, null, null);

    private static ProfessionalProfile CreateProfile(Guid ownerUserId, string name) => new(Guid.NewGuid(), ownerUserId, ValidContactInfo(name), "Summary.", DateTime.UtcNow);

    private static CVPresentation CreatePresentation(Guid ownerUserId, Guid professionalProfileId) => new(
        Guid.NewGuid(), ownerUserId, professionalProfileId, "UK — Senior Backend Engineer", "United Kingdom", "Senior Backend Engineer",
        "en-GB", "modern-one-page", null, false, true, true, false, "dd MMM yyyy", 2, DateTime.UtcNow);

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersProfile_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new ProfessionalProfileRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var profileA = CreateProfile(ownerAId, "Ada Lovelace");

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(profileA, CancellationToken.None), CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await repository.GetByOwnerUserIdAsync(ownerAId, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersProfile_EvenWithARawUpdateBypassingTheRepositoryFilter()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new ProfessionalProfileRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var profileA = CreateProfile(ownerAId, "Ada Lovelace");
        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, () => repository.AddAsync(profileA, CancellationToken.None), CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE professional_profiles SET summary_markdown = 'hacked' WHERE id = {profileA.Id}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    /// <summary>skills has no owner_user_id column — isolation is transitive through professional_profiles, exactly like study_reviews in the Phase 1 test.</summary>
    [Fact]
    public async Task Owner_CannotMutateAnotherOwnersSkill_TransitivelyThroughItsParentProfile()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var repository = new ProfessionalProfileRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var profileA = CreateProfile(ownerAId, "Ada Lovelace");
        var skillId = Guid.NewGuid();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, async () =>
        {
            await repository.AddAsync(profileA, CancellationToken.None);
            profileA.ReplaceSkills([new Skill(skillId, "C#", SkillCategory.Language, null)], DateTime.UtcNow);
            await repository.SaveChangesAsync(CancellationToken.None);
        }, CancellationToken.None);

        var rowsAffected = 0;
        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE skills SET display_name = 'hacked' WHERE id = {skillId}", CancellationToken.None);
        }, CancellationToken.None);

        Assert.Equal(0, rowsAffected);
    }

    [Fact]
    public async Task Owner_CannotReadAnotherOwnersCVPresentation_ThroughTheRepository()
    {
        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var profileRepository = new ProfessionalProfileRepository(dbContext);
        var presentationRepository = new CVPresentationRepository(dbContext);
        var ownerAId = await CreateUserAsync();
        var ownerBId = await CreateUserAsync();
        var profileA = CreateProfile(ownerAId, "Ada Lovelace");
        var presentationA = CreatePresentation(ownerAId, profileA.Id);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerAId, async () =>
        {
            await profileRepository.AddAsync(profileA, CancellationToken.None);
            await presentationRepository.AddAsync(presentationA, CancellationToken.None);
        }, CancellationToken.None);

        await rlsSessionContext.RunInOwnerScopeAsync(ownerBId, async () =>
        {
            var found = await presentationRepository.GetByIdAsync(ownerAId, presentationA.Id, CancellationToken.None);
            Assert.Null(found);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WithoutAnyOwnerContext_QueryingProfilesAndPresentationsReturnsNoRows_EvenThoughRowsExist()
    {
        await using var setupDbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(setupDbContext, NullLogger<RlsSessionContext>.Instance);
        var profileRepository = new ProfessionalProfileRepository(setupDbContext);
        var presentationRepository = new CVPresentationRepository(setupDbContext);
        var ownerUserId = await CreateUserAsync();
        var profile = CreateProfile(ownerUserId, "Ada Lovelace");

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            await profileRepository.AddAsync(profile, CancellationToken.None);
            await presentationRepository.AddAsync(CreatePresentation(ownerUserId, profile.Id), CancellationToken.None);
        }, CancellationToken.None);

        // A fresh DbContext/connection that never calls set_config at all — current_setting(...,
        // true) returns NULL, and owner_user_id = NULL is never true, so both queries must see
        // zero rows regardless of how many rows actually exist for any owner.
        await using var noContextDbContext = CreateAppDbContext();
        Assert.Equal(0, await noContextDbContext.ProfessionalProfiles.CountAsync());
        Assert.Equal(0, await noContextDbContext.CVPresentations.CountAsync());
    }

    [Fact]
    public async Task TheSetupScripts_RemainSafe_WhenAppliedASecondTime()
    {
        // setup-local-db.ps1 re-applies 001/002/003/004 on every invocation, not just against a
        // fresh volume — this proves 004_rls_phase2.sql specifically is safe to run twice, beyond
        // the single application InitializeAsync already performed for every other test here.
        await ApplyBootstrapScriptsAsync();
        await ApplyRlsScriptsAsync();

        await using var dbContext = CreateAppDbContext();
        var rlsSessionContext = new RlsSessionContext(dbContext, NullLogger<RlsSessionContext>.Instance);
        var profileRepository = new ProfessionalProfileRepository(dbContext);
        var presentationRepository = new CVPresentationRepository(dbContext);
        var ownerUserId = await CreateUserAsync();

        await rlsSessionContext.RunInOwnerScopeAsync(ownerUserId, async () =>
        {
            var profile = CreateProfile(ownerUserId, "Still Works");
            await profileRepository.AddAsync(profile, CancellationToken.None);
            var presentation = CreatePresentation(ownerUserId, profile.Id);
            await presentationRepository.AddAsync(presentation, CancellationToken.None);

            Assert.NotNull(await profileRepository.GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None));
            Assert.NotNull(await presentationRepository.GetByIdAsync(ownerUserId, presentation.Id, CancellationToken.None));
        }, CancellationToken.None);
    }
}
