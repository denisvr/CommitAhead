using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class DeleteJobAnalysisUseCaseTests
{
    private static JobAnalysis CreateAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingAnalysis_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new DeleteJobAnalysisUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingAnalysis_ReturnsNotFound()
    {
        var repository = new FakeJobAnalysisRepository();
        var useCase = new DeleteJobAnalysisUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.NotFound, result);
    }
}
