using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.EvidenceLinks;

[Collection(PostgresCollection.Name)]
public sealed class EvidenceLinkRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public EvidenceLinkRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static StudyItem CreateStudyItem(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "PostgreSQL Indexing", StudyItemCategory.Theory, 3, 2, ["databases"],
        new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);

    [Fact]
    public async Task AddThenExists_RoundTripsTheLink()
    {
        var studyItemRepository = new StudyItemRepository(_dbContext);
        var evidenceLinkRepository = new EvidenceLinkRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var studyItem = CreateStudyItem(ownerUserId);
        await studyItemRepository.AddAsync(studyItem, CancellationToken.None);
        var sourceId = Guid.NewGuid();

        Assert.False(await evidenceLinkRepository.ExistsAsync(ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, CancellationToken.None));

        var link = new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, 3, "Directly demonstrates this skill.", DateTime.UtcNow);
        await evidenceLinkRepository.AddAsync(link, CancellationToken.None);

        Assert.True(await evidenceLinkRepository.ExistsAsync(ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForADifferentOwner()
    {
        var studyItemRepository = new StudyItemRepository(_dbContext);
        var evidenceLinkRepository = new EvidenceLinkRepository(_dbContext);
        var ownerAId = await TestUsers.CreateAsync(_dbContext);
        var ownerBId = await TestUsers.CreateAsync(_dbContext);
        var studyItem = CreateStudyItem(ownerAId);
        await studyItemRepository.AddAsync(studyItem, CancellationToken.None);
        var sourceId = Guid.NewGuid();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerAId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, 3, "Rationale.", DateTime.UtcNow), CancellationToken.None);

        Assert.False(await evidenceLinkRepository.ExistsAsync(ownerBId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, CancellationToken.None));
    }

    /// <summary>Proves the database's own unique index on (SourceType, SourceId, TargetStudyItemId) is real, not just an application-level check, and that AddAsync maps its violation to EvidenceLinkConflictException.</summary>
    [Fact]
    public async Task AddAsync_WithADuplicateSourceAndTarget_ThrowsEvidenceLinkConflictException()
    {
        var studyItemRepository = new StudyItemRepository(_dbContext);
        var evidenceLinkRepository = new EvidenceLinkRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var studyItem = CreateStudyItem(ownerUserId);
        await studyItemRepository.AddAsync(studyItem, CancellationToken.None);
        var sourceId = Guid.NewGuid();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, 3, "First link.", DateTime.UtcNow), CancellationToken.None);

        var duplicate = new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, studyItem.Id, 4, "Second link.", DateTime.UtcNow);

        await Assert.ThrowsAsync<EvidenceLinkConflictException>(() => evidenceLinkRepository.AddAsync(duplicate, CancellationToken.None));
    }

    /// <summary>ADR-0011 source-deletion cleanup — bulk-deletes only the matching source's links, leaving others (different source, different owner) untouched.</summary>
    [Fact]
    public async Task DeleteAllForSourceAsync_RemovesOnlyLinksForThatExactSource()
    {
        var studyItemRepository = new StudyItemRepository(_dbContext);
        var evidenceLinkRepository = new EvidenceLinkRepository(_dbContext);
        var ownerAId = await TestUsers.CreateAsync(_dbContext);
        var ownerBId = await TestUsers.CreateAsync(_dbContext);
        var studyItem = CreateStudyItem(ownerAId);
        await studyItemRepository.AddAsync(studyItem, CancellationToken.None);
        var targetSourceId = Guid.NewGuid();
        var otherSourceId = Guid.NewGuid();

        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerAId, EvidenceSourceType.JobAnalysis, targetSourceId, studyItem.Id, 3, "Matches.", DateTime.UtcNow), CancellationToken.None);
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerAId, EvidenceSourceType.JobAnalysis, otherSourceId, studyItem.Id, 3, "Matches.", DateTime.UtcNow), CancellationToken.None);
        var otherOwnerStudyItem = CreateStudyItem(ownerBId);
        await studyItemRepository.AddAsync(otherOwnerStudyItem, CancellationToken.None);
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerBId, EvidenceSourceType.JobAnalysis, targetSourceId, otherOwnerStudyItem.Id, 3, "Matches.", DateTime.UtcNow), CancellationToken.None);

        await evidenceLinkRepository.DeleteAllForSourceAsync(ownerAId, EvidenceSourceType.JobAnalysis, targetSourceId, CancellationToken.None);

        Assert.False(await evidenceLinkRepository.ExistsAsync(ownerAId, EvidenceSourceType.JobAnalysis, targetSourceId, studyItem.Id, CancellationToken.None));
        Assert.True(await evidenceLinkRepository.ExistsAsync(ownerAId, EvidenceSourceType.JobAnalysis, otherSourceId, studyItem.Id, CancellationToken.None));
        Assert.True(await evidenceLinkRepository.ExistsAsync(ownerBId, EvidenceSourceType.JobAnalysis, targetSourceId, otherOwnerStudyItem.Id, CancellationToken.None));
    }
}
