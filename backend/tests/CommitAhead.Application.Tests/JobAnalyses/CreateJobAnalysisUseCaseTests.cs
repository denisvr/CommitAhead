using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class CreateJobAnalysisUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidInput_CreatesAnAnalysisOwnedByTheCurrentUser()
    {
        var repository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var useCase = new CreateJobAnalysisUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync("Senior Backend Engineer", new PastedText("Job posting text."), "Some notes.", CancellationToken.None);

        var created = Assert.Single(repository.Analyses);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
        Assert.Equal("Senior Backend Engineer", created.Title);
    }
}
