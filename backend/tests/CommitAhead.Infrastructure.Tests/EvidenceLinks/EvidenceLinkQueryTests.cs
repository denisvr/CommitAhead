using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.EvidenceLinks;

[Collection(PostgresCollection.Name)]
public class EvidenceLinkQueryTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public EvidenceLinkQueryTests(PostgresContainerFixture fixture)
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

    // target_study_item_id has a real FK to study_items — a link can only target a StudyItem
    // that actually exists.
    private async Task<Guid> CreateStudyItemAsync(Guid ownerUserId)
    {
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 3, 3, [], new TheoryDetails("s", [], [], []), Now);
        await new StudyItemRepository(_dbContext).AddAsync(item, CancellationToken.None);
        return item.Id;
    }

    private static EvidenceLink CreateLink(Guid ownerUserId, Guid targetStudyItemId, decimal weight) =>
        new(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), targetStudyItemId, weight, "Mentioned in job posting", Now);

    [Fact]
    public async Task GetDemandAsync_WithNoLinks_ReturnsZero()
    {
        var ownerUserId = Guid.NewGuid();
        var targetStudyItemId = await CreateStudyItemAsync(ownerUserId);
        var query = new EvidenceLinkQuery(_dbContext);

        var demand = await query.GetDemandAsync(ownerUserId, targetStudyItemId, CancellationToken.None);

        Assert.Equal(0m, demand);
    }

    [Fact]
    public async Task GetDemandAsync_SumsWeightsTargetingTheItem_ClampedAtFive()
    {
        var ownerUserId = Guid.NewGuid();
        var targetStudyItemId = await CreateStudyItemAsync(ownerUserId);
        var otherStudyItemId = await CreateStudyItemAsync(ownerUserId);
        _dbContext.EvidenceLinks.AddRange(
            CreateLink(ownerUserId, targetStudyItemId, 3m),
            CreateLink(ownerUserId, targetStudyItemId, 4m),
            CreateLink(ownerUserId, otherStudyItemId, 5m));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var query = new EvidenceLinkQuery(_dbContext);
        var demand = await query.GetDemandAsync(ownerUserId, targetStudyItemId, CancellationToken.None);

        Assert.Equal(5m, demand);
    }

    [Fact]
    public async Task GetDemandAsync_ScopedToADifferentOwner_ReturnsZero()
    {
        var ownerUserId = Guid.NewGuid();
        var targetStudyItemId = await CreateStudyItemAsync(ownerUserId);
        _dbContext.EvidenceLinks.Add(CreateLink(ownerUserId, targetStudyItemId, 3m));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var query = new EvidenceLinkQuery(_dbContext);
        var demand = await query.GetDemandAsync(Guid.NewGuid(), targetStudyItemId, CancellationToken.None);

        Assert.Equal(0m, demand);
    }

    [Fact]
    public async Task AnyTargetingStudyItemAsync_WithNoLinks_ReturnsFalse()
    {
        var ownerUserId = Guid.NewGuid();
        var targetStudyItemId = await CreateStudyItemAsync(ownerUserId);
        var query = new EvidenceLinkQuery(_dbContext);

        Assert.False(await query.AnyTargetingStudyItemAsync(ownerUserId, targetStudyItemId, CancellationToken.None));
    }

    [Fact]
    public async Task AnyTargetingStudyItemAsync_WithALink_ReturnsTrue()
    {
        var ownerUserId = Guid.NewGuid();
        var targetStudyItemId = await CreateStudyItemAsync(ownerUserId);
        _dbContext.EvidenceLinks.Add(CreateLink(ownerUserId, targetStudyItemId, 1m));
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var query = new EvidenceLinkQuery(_dbContext);

        Assert.True(await query.AnyTargetingStudyItemAsync(ownerUserId, targetStudyItemId, CancellationToken.None));
    }
}
