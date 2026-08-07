using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class GetJobAnalysisUseCaseTests
{
    private static JobAnalysis CreateAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingAnalysis_ReturnsItsProjection()
    {
        var repository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new GetJobAnalysisUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(analysis.Id, result.Id);
        Assert.Equal("Title", result.Title);
    }

    [Fact]
    public async Task ExecuteAsync_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new FakeJobAnalysisRepository();
        var analysis = CreateAnalysis(Guid.NewGuid());
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new GetJobAnalysisUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Null(result);
    }
}
