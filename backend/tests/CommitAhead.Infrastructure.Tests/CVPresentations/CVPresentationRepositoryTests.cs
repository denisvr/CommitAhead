using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.CVPresentations;

[Collection(PostgresCollection.Name)]
public class CVPresentationRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public CVPresentationRepositoryTests(PostgresContainerFixture fixture)
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

    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private async Task<Guid> CreateProfileAsync(Guid ownerUserId)
    {
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Summary.", DateTime.UtcNow);
        await new ProfessionalProfileRepository(_dbContext).AddAsync(profile, CancellationToken.None);
        return profile.Id;
    }

    private static CVPresentation CreatePresentation(Guid ownerUserId, Guid professionalProfileId) => new(
        Guid.NewGuid(), ownerUserId, professionalProfileId, "UK — Senior Backend Engineer", "United Kingdom", "Senior Backend Engineer",
        "en-GB", "modern-one-page", null, false, true, true, false, "dd MMM yyyy", 2, DateTime.UtcNow);

    [Fact]
    public async Task AddThenGetById_RoundTripsEveryFieldAndSelection()
    {
        var repository = new CVPresentationRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profileId = await CreateProfileAsync(ownerUserId);
        var presentation = CreatePresentation(ownerUserId, profileId);
        var experienceId = Guid.NewGuid();
        presentation.ReplaceExperienceSelections([experienceId], DateTime.UtcNow);
        presentation.ReplaceProfileLinkSelections([Guid.NewGuid(), Guid.NewGuid()], DateTime.UtcNow);

        await repository.AddAsync(presentation, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new CVPresentationRepository(reloadDbContext).GetByIdAsync(ownerUserId, presentation.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("UK — Senior Backend Engineer", reloaded.Label);
        Assert.Equal(profileId, reloaded.ProfessionalProfileId);
        Assert.Equal([experienceId], reloaded.ExperienceSelections);
        Assert.Equal(2, reloaded.ProfileLinkSelections.Count);
    }

    [Fact]
    public async Task GetById_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new CVPresentationRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profileId = await CreateProfileAsync(ownerUserId);
        var presentation = CreatePresentation(ownerUserId, profileId);
        await repository.AddAsync(presentation, CancellationToken.None);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), presentation.Id, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTheOwnersPresentations()
    {
        var repository = new CVPresentationRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var otherOwnerUserId = await TestUsers.CreateAsync(_dbContext);
        var profileId = await CreateProfileAsync(ownerUserId);
        var otherProfileId = await CreateProfileAsync(otherOwnerUserId);
        await repository.AddAsync(CreatePresentation(ownerUserId, profileId), CancellationToken.None);
        await repository.AddAsync(CreatePresentation(otherOwnerUserId, otherProfileId), CancellationToken.None);

        var results = await repository.GetAllAsync(ownerUserId, CancellationToken.None);

        Assert.Single(results);
        Assert.All(results, presentation => Assert.Equal(ownerUserId, presentation.OwnerUserId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesThePresentation()
    {
        var repository = new CVPresentationRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var profileId = await CreateProfileAsync(ownerUserId);
        var presentation = CreatePresentation(ownerUserId, profileId);
        await repository.AddAsync(presentation, CancellationToken.None);

        await repository.DeleteAsync(presentation, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(ownerUserId, presentation.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_ReferencingANonexistentProfile_IsRejectedByTheDatabase()
    {
        var repository = new CVPresentationRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var presentation = CreatePresentation(ownerUserId, Guid.NewGuid());

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => repository.AddAsync(presentation, CancellationToken.None));
    }
}
