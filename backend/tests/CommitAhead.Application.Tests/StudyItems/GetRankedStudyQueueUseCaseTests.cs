using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class GetRankedStudyQueueUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoOverride_QueriesWithDefaultWeights()
    {
        var ownerUserId = Guid.NewGuid();
        var query = new FakeRankedStudyQueueQuery();
        var useCase = new GetRankedStudyQueueUseCase(query, new FakeScoringConfigRepository(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(ownerUserId, query.LastOwnerUserId);
        Assert.Equal(ScoringWeights.Default.ImportanceWeight, query.LastWeights!.ImportanceWeight);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnOverride_QueriesWithTheOverrideWeights()
    {
        var ownerUserId = Guid.NewGuid();
        var scoringConfigRepository = new FakeScoringConfigRepository();
        await scoringConfigRepository.SetOverrideAsync(ownerUserId, new ScoringWeights(50, 30, 20), CancellationToken.None);
        var query = new FakeRankedStudyQueueQuery();
        var useCase = new GetRankedStudyQueueUseCase(query, scoringConfigRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(50, query.LastWeights!.ImportanceWeight);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsWhateverTheQueryProduces()
    {
        var expected = new List<RankedStudyItem>
        {
            new(Guid.NewGuid(), "Title", StudyItemCategory.Theory, 3, 2m, 0m, 60, null, null, null, DateTime.UtcNow),
        };
        var query = new FakeRankedStudyQueueQuery { ResultToReturn = expected };
        var useCase = new GetRankedStudyQueueUseCase(query, new FakeScoringConfigRepository(), new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Same(expected, result);
    }
}
