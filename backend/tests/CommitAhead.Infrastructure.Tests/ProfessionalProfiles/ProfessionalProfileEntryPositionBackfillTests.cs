using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.ProfessionalProfiles;

/// <summary>
/// Proves the corrective SQL in
/// <c>Migrations/20260820144624_BackfillProfessionalProfileEntryPositions</c> does what its
/// migration comment claims: a multi-row group that collapsed to position 0 (the bug in the prior
/// migration, which only ever applied the schema default) gets distinct, deterministic positions,
/// while a group that already has real positions from an actual <c>Replace*</c> save is left
/// alone. The corrective SQL is re-typed here rather than invoked through the migration history
/// mechanism — <see cref="PostgresContainerFixture"/> applies every migration once, against an
/// empty database, before any test's rows exist, so the migration itself has nothing to correct by
/// the time a test runs; exercising the same statement directly is the only way to test its
/// behaviour against rows a test controls.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ProfessionalProfileEntryPositionBackfillTests : IAsyncLifetime
{
    private const string BackfillExperiencePositionsSql = """
        WITH untouched_groups AS (
            SELECT professional_profile_id
            FROM experience_entries
            GROUP BY professional_profile_id
            HAVING COUNT(*) > 1 AND bool_and(position = 0)
        ),
        ranked AS (
            SELECT t.id, ROW_NUMBER() OVER (PARTITION BY t.professional_profile_id ORDER BY t.ctid) - 1 AS new_position
            FROM experience_entries t
            JOIN untouched_groups g ON g.professional_profile_id = t.professional_profile_id
        )
        UPDATE experience_entries t
        SET position = ranked.new_position
        FROM ranked
        WHERE t.id = ranked.id;
        """;

    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public ProfessionalProfileEntryPositionBackfillTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = CreateDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private CommitAheadDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);

    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static ExperienceEntry Entry(string company, int startYear) =>
        new(Guid.NewGuid(), company, null, "Engineer", EmploymentType.Permanent, new YearMonth(startYear, 1), null, null, WorkMode.Remote, "Summary.", [], []);

    [Fact]
    public async Task MultiRowGroupCollapsedToPositionZero_GetsDistinctDeterministicPositionsOnReload()
    {
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        profile.ReplaceExperience([Entry("Alpha", 2018), Entry("Beta", 2019), Entry("Gamma", 2020)], DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);

        // Simulate the bug the corrective migration exists for: every row in this profile's
        // experience collection collapsed to the schema default instead of a real position.
        await _dbContext.Database.ExecuteSqlRawAsync("UPDATE experience_entries SET position = 0 WHERE professional_profile_id = {0}", profile.Id);

        await _dbContext.Database.ExecuteSqlRawAsync(BackfillExperiencePositionsSql);

        await using var firstReadContext = CreateDbContext();
        var firstReload = await new ProfessionalProfileRepository(firstReadContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);
        await using var secondReadContext = CreateDbContext();
        var secondReload = await new ProfessionalProfileRepository(secondReadContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);

        var firstPositions = firstReload!.Experience.Select(entry => entry.Position).OrderBy(position => position).ToArray();
        Assert.Equal([0, 1, 2], firstPositions);

        var firstOrder = firstReload.Experience.Select(entry => entry.Company).ToArray();
        var secondOrder = secondReload!.Experience.Select(entry => entry.Company).ToArray();
        Assert.Equal(firstOrder, secondOrder);
    }

    [Fact]
    public async Task GroupWithRealPositionsFromAnActualReplace_IsNotTouchedByTheCorrectiveSql()
    {
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        // A normal ReplaceExperience call already stamps distinct positions (0, 1) — this group
        // must never look like the all-zero bug the corrective SQL targets.
        profile.ReplaceExperience([Entry("Alpha", 2018), Entry("Beta", 2019)], DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);

        await _dbContext.Database.ExecuteSqlRawAsync(BackfillExperiencePositionsSql);

        await using var reloadContext = CreateDbContext();
        var reloaded = await new ProfessionalProfileRepository(reloadContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);

        Assert.Equal(["Alpha", "Beta"], reloaded!.Experience.Select(entry => entry.Company));
        Assert.Equal([0, 1], reloaded.Experience.Select(entry => entry.Position));
    }
}
