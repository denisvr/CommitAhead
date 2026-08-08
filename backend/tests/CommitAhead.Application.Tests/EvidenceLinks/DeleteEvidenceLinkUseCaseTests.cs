using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Application.Tests.AnalysisDrafts;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.Tests.EvidenceLinks;

public class DeleteEvidenceLinkUseCaseTests
{
    private static EvidenceLink CreateLink(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.", DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingLink_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeEvidenceLinkRepository();
        var ownerUserId = Guid.NewGuid();
        var link = CreateLink(ownerUserId);
        await repository.AddAsync(link, CancellationToken.None);
        var useCase = new DeleteEvidenceLinkUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(link.Id, CancellationToken.None);

        Assert.Equal(EvidenceLinkMutationResult.Success, result);
        Assert.Empty(repository.Links);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingLink_ReturnsNotFound()
    {
        var repository = new FakeEvidenceLinkRepository();
        var useCase = new DeleteEvidenceLinkUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(EvidenceLinkMutationResult.NotFound, result);
    }

    /// <summary>ADR-0015 owner isolation — a link belonging to a different owner is invisible, not deletable.</summary>
    [Fact]
    public async Task ExecuteAsync_WithALinkOwnedByAnotherUser_ReturnsNotFoundAndLeavesItIntact()
    {
        var repository = new FakeEvidenceLinkRepository();
        var link = CreateLink(Guid.NewGuid());
        await repository.AddAsync(link, CancellationToken.None);
        var useCase = new DeleteEvidenceLinkUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "other@example.com" });

        var result = await useCase.ExecuteAsync(link.Id, CancellationToken.None);

        Assert.Equal(EvidenceLinkMutationResult.NotFound, result);
        Assert.Single(repository.Links);
    }
}
