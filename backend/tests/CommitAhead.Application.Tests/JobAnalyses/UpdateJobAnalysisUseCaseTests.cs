using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class UpdateJobAnalysisUseCaseTests
{
    private static JobAnalysis CreateAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingAnalysis_UpdatesItAndReturnsSuccess()
    {
        var repository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new UpdateJobAnalysisUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(analysis.Id, "New title", "New notes.", CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Equal("New title", analysis.Title);
        Assert.Equal("New notes.", analysis.NotesMarkdown);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingAnalysis_ReturnsNotFound()
    {
        var repository = new FakeJobAnalysisRepository();
        var useCase = new UpdateJobAnalysisUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "Title", null, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.NotFound, result);
    }
}
