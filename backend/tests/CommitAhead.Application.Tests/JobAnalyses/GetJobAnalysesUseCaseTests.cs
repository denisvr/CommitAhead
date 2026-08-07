using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class GetJobAnalysesUseCaseTests
{
    private static JobAnalysis CreateAnalysis(Guid ownerUserId, string title) => new(
        Guid.NewGuid(), ownerUserId, title, new PastedText("Job posting text."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyTheCurrentOwnersAnalyses()
    {
        var repository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(CreateAnalysis(ownerUserId, "Mine"), CancellationToken.None);
        await repository.AddAsync(CreateAnalysis(Guid.NewGuid(), "Someone else's"), CancellationToken.None);
        var useCase = new GetJobAnalysesUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["Mine"], results.Select(r => r.Title));
    }
}
