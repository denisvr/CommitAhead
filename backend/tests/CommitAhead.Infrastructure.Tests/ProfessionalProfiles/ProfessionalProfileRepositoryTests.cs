using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.ProfessionalProfiles;

[Collection(PostgresCollection.Name)]
public class ProfessionalProfileRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public ProfessionalProfileRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private CommitAheadDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);

    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", "+44 20 7946 0958", "London, UK", "photos/ada.jpg");

    [Fact]
    public async Task AddThenGetByOwnerUserId_RoundTripsEveryChildCollectionAndYearMonthFields()
    {
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Backend engineer.", DateTime.UtcNow);
        var skill = new Skill(Guid.NewGuid(), "C#", SkillCategory.Language, SkillProficiency.Expert);
        profile.ReplaceSkills([skill], DateTime.UtcNow);
        profile.ReplaceExperience(
            [new ExperienceEntry(
                Guid.NewGuid(), "Acme", "Client Co", "Engineer", EmploymentType.Permanent,
                new YearMonth(2020, 1), new YearMonth(2023, 6), "Remote", WorkMode.Remote, "Summary", ["Shipped v2"], [skill.Id])],
            DateTime.UtcNow);
        profile.ReplaceEducation([new EducationEntry(Guid.NewGuid(), "MIT", "BSc", "CS", new YearMonth(2016, 9), null, "Cambridge, MA", "Details")], DateTime.UtcNow);
        profile.ReplaceLanguages([new LanguageEntry(Guid.NewGuid(), "English", LanguageProficiency.Native, null)], DateTime.UtcNow);
        profile.ReplaceCertifications(
            [new CertificationEntry(Guid.NewGuid(), "AWS Certified Developer", "Amazon", new YearMonth(2022, 3), null, "ABC123", "https://aws.amazon.com/verify")],
            DateTime.UtcNow);
        profile.ReplaceProjects([new ProjectEntry(Guid.NewGuid(), "CommitAhead", "Author", null, null, "An interview-prep app.", "https://github.com/example", [skill.Id])], DateTime.UtcNow);
        profile.ReplaceProfileLinks([new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "https://github.com/example")], DateTime.UtcNow);

        await repository.AddAsync(profile, CancellationToken.None);

        await using var reloadDbContext = CreateDbContext();
        var reloaded = await new ProfessionalProfileRepository(reloadDbContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("Ada Lovelace", reloaded.ContactInfo.Name);
        Assert.Equal("ada@example.com", reloaded.ContactInfo.Email);

        var experience = Assert.Single(reloaded.Experience);
        Assert.Equal("Acme", experience.Company);
        Assert.Equal(new YearMonth(2020, 1), experience.StartDate);
        Assert.Equal(new YearMonth(2023, 6), experience.EndDate);
        Assert.Equal(["Shipped v2"], experience.Achievements);
        Assert.Equal([skill.Id], experience.SkillIds);

        var education = Assert.Single(reloaded.Education);
        Assert.Equal(new YearMonth(2016, 9), education.StartDate);
        Assert.Null(education.EndDate);

        Assert.Single(reloaded.Skills);
        Assert.Single(reloaded.Languages);

        var certification = Assert.Single(reloaded.Certifications);
        Assert.Equal(new YearMonth(2022, 3), certification.IssuedAt);
        Assert.Null(certification.ExpiresAt);

        var project = Assert.Single(reloaded.Projects);
        Assert.Equal([skill.Id], project.SkillIds);

        Assert.Single(reloaded.ProfileLinks);
    }

    [Fact]
    public async Task AddAsync_ASecondProfileForTheSameOwner_ViolatesTheUniqueIndex()
    {
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        await repository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "First.", DateTime.UtcNow), CancellationToken.None);

        await using var otherDbContext = CreateDbContext();
        var otherRepository = new ProfessionalProfileRepository(otherDbContext);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => otherRepository.AddAsync(new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Second.", DateTime.UtcNow), CancellationToken.None));
    }

    [Fact]
    public async Task ReplaceExperience_EditingAnExistingEntrysDataWhilePreservingItsId_RoundTripsTheChangeWithoutDuplicating()
    {
        // The open EF question this slice's plan flagged: ProfessionalProfile.ReplaceExperience
        // reassigns the backing list to brand-new ExperienceEntry instances built by the caller,
        // not the same objects EF loaded. An edited entry keeping its Id (expected — CVPresentation
        // selections reference entries by Id and must not go stale across a profile edit) must not
        // make EF's identity map see two different instances claiming the same key.
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var experienceId = Guid.NewGuid();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        profile.ReplaceExperience(
            [new ExperienceEntry(experienceId, "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Old summary", [], [])],
            DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);

        await using var editDbContext = CreateDbContext();
        var editRepository = new ProfessionalProfileRepository(editDbContext);
        var loadedForEdit = await editRepository.GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);
        var editedEntry = new ExperienceEntry(
            experienceId, "Acme Corp Renamed", null, "Senior Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), new YearMonth(2024, 1), null, WorkMode.Hybrid, "New summary", [], []);
        loadedForEdit!.ReplaceExperience([editedEntry], DateTime.UtcNow);
        await editRepository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = CreateDbContext();
        var reloaded = await new ProfessionalProfileRepository(reloadDbContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);

        var experience = Assert.Single(reloaded!.Experience);
        Assert.Equal(experienceId, experience.Id);
        Assert.Equal("Acme Corp Renamed", experience.Company);
        Assert.Equal("Senior Engineer", experience.Role);
        Assert.Equal(new YearMonth(2024, 1), experience.EndDate);
    }

    [Fact]
    public async Task ReplaceEducation_RemovingAnEntryAndAddingAnother_RoundTripsBoth()
    {
        var repository = new ProfessionalProfileRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        profile.ReplaceEducation([new EducationEntry(Guid.NewGuid(), "MIT", "BSc", null, null, null, null, null)], DateTime.UtcNow);
        await repository.AddAsync(profile, CancellationToken.None);

        await using var editDbContext = CreateDbContext();
        var editRepository = new ProfessionalProfileRepository(editDbContext);
        var loadedForEdit = await editRepository.GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);
        loadedForEdit!.ReplaceEducation([new EducationEntry(Guid.NewGuid(), "Stanford", "MSc", null, null, null, null, null)], DateTime.UtcNow);
        await editRepository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = CreateDbContext();
        var reloaded = await new ProfessionalProfileRepository(reloadDbContext).GetByOwnerUserIdAsync(ownerUserId, CancellationToken.None);

        var education = Assert.Single(reloaded!.Education);
        Assert.Equal("Stanford", education.Institution);
    }
}
