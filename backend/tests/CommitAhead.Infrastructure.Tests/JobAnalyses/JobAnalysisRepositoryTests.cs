using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

[Collection(PostgresCollection.Name)]
public class JobAnalysisRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public JobAnalysisRepositoryTests(PostgresContainerFixture fixture)
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

    private static JobAnalysis CreateAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting text."), "Some notes.", DateTime.UtcNow);

    [Fact]
    public async Task AddThenGetById_RoundTripsTheJobSourceAndChildren()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = CreateAnalysis(ownerUserId);
        var requirement = new JobRequirement(Guid.NewGuid(), "5+ years of C#.", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience.");
        analysis.AddRequirement(requirement, DateTime.UtcNow);
        var gap = new JobGap(Guid.NewGuid(), requirement.Id, JobGapMatchLevel.Partial, JobGapSeverity.Medium, "Only 3 years of C# so far.");
        analysis.AddGap(gap, DateTime.UtcNow);

        await repository.AddAsync(analysis, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new JobAnalysisRepository(reloadDbContext).GetByIdAsync(ownerUserId, analysis.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("Senior Backend Engineer", reloaded.Title);
        var reloadedSource = Assert.IsType<PastedText>(reloaded.JobSource);
        Assert.Equal("Job posting text.", reloadedSource.Content);
        var reloadedRequirement = Assert.Single(reloaded.Requirements);
        Assert.Equal(requirement.Id, reloadedRequirement.Id);
        var reloadedGap = Assert.Single(reloaded.Gaps);
        Assert.Equal(gap.Id, reloadedGap.Id);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsAnUploadedFileJobSource()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = new JobAnalysis(
            Guid.NewGuid(), ownerUserId, "Title", new UploadedFile("quarantine/abc", "posting.pdf", "application/pdf", "Extracted text."), null, DateTime.UtcNow);

        await repository.AddAsync(analysis, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new JobAnalysisRepository(reloadDbContext).GetByIdAsync(ownerUserId, analysis.Id, CancellationToken.None);

        var reloadedSource = Assert.IsType<UploadedFile>(reloaded!.JobSource);
        Assert.Equal("quarantine/abc", reloadedSource.StorageObjectKey);
        Assert.Equal("application/pdf", reloadedSource.MimeType);
    }

    [Fact]
    public async Task GetById_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = CreateAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), analysis.Id, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTheOwnersAnalyses()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var otherOwnerUserId = await TestUsers.CreateAsync(_dbContext);
        await repository.AddAsync(CreateAnalysis(ownerUserId), CancellationToken.None);
        await repository.AddAsync(CreateAnalysis(otherOwnerUserId), CancellationToken.None);

        var results = await repository.GetAllAsync(ownerUserId, CancellationToken.None);

        Assert.Single(results);
        Assert.All(results, analysis => Assert.Equal(ownerUserId, analysis.OwnerUserId));
    }

    /// <summary>
    /// RemoveRequirement's atomic requirement+related-gap cleanup (JobAnalysis.cs) must persist as
    /// two real DELETEs, not just an in-memory change — this is the actual database round trip,
    /// not the Domain.Tests coverage of the in-memory invariant itself.
    /// </summary>
    [Fact]
    public async Task RemoveRequirement_PersistsTheRequirementAndItsRelatedGapRemoval()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = CreateAnalysis(ownerUserId);
        var requirement = new JobRequirement(Guid.NewGuid(), "5+ years of C#.", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience.");
        analysis.AddRequirement(requirement, DateTime.UtcNow);
        var gap = new JobGap(Guid.NewGuid(), requirement.Id, JobGapMatchLevel.Partial, JobGapSeverity.Medium, "Only 3 years of C# so far.");
        analysis.AddGap(gap, DateTime.UtcNow);
        await repository.AddAsync(analysis, CancellationToken.None);

        analysis.RemoveRequirement(requirement.Id, DateTime.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new JobAnalysisRepository(reloadDbContext).GetByIdAsync(ownerUserId, analysis.Id, CancellationToken.None);
        Assert.Empty(reloaded!.Requirements);
        Assert.Empty(reloaded.Gaps);
    }

    /// <summary>
    /// The in-memory invariant (JobAnalysis.AddGap) already rejects an invalid RequirementId
    /// before anything is persisted — these two tests instead bypass the aggregate entirely
    /// (raw <see cref="DbContext.Set{TEntity}"/> + a manually-set shadow FK) to prove the
    /// composite FK added in JobGapConfiguration rejects the same invalid write at the database
    /// level too, as defense-in-depth against a future bug that skips AddGap.
    /// </summary>
    [Fact]
    public async Task SaveChanges_WithAGapReferencingANonexistentRequirement_ThrowsFromTheDatabaseConstraint()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = CreateAnalysis(ownerUserId);
        await new JobAnalysisRepository(_dbContext).AddAsync(analysis, CancellationToken.None);

        var invalidGap = new JobGap(Guid.NewGuid(), Guid.NewGuid(), JobGapMatchLevel.Partial, JobGapSeverity.Medium, "References a requirement that doesn't exist.");
        _dbContext.Set<JobGap>().Add(invalidGap);
        _dbContext.Entry(invalidGap).Property("JobAnalysisId").CurrentValue = analysis.Id;

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SaveChanges_WithAGapReferencingARequirementFromADifferentAnalysis_ThrowsFromTheDatabaseConstraint()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysisWithRequirement = CreateAnalysis(ownerUserId);
        var requirement = new JobRequirement(Guid.NewGuid(), "5+ years of C#.", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience.");
        analysisWithRequirement.AddRequirement(requirement, DateTime.UtcNow);
        var repository = new JobAnalysisRepository(_dbContext);
        await repository.AddAsync(analysisWithRequirement, CancellationToken.None);
        var otherAnalysis = CreateAnalysis(ownerUserId);
        await repository.AddAsync(otherAnalysis, CancellationToken.None);

        var crossAnalysisGap = new JobGap(Guid.NewGuid(), requirement.Id, JobGapMatchLevel.Partial, JobGapSeverity.Medium, "References a requirement from a different analysis.");
        _dbContext.Set<JobGap>().Add(crossAnalysisGap);
        _dbContext.Entry(crossAnalysisGap).Property("JobAnalysisId").CurrentValue = otherAnalysis.Id;

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheAnalysisAndItsChildren()
    {
        var repository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var analysis = CreateAnalysis(ownerUserId);
        var requirement = new JobRequirement(Guid.NewGuid(), "5+ years of C#.", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience.");
        analysis.AddRequirement(requirement, DateTime.UtcNow);
        await repository.AddAsync(analysis, CancellationToken.None);

        await repository.DeleteAsync(analysis, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(ownerUserId, analysis.Id, CancellationToken.None));
    }
}
