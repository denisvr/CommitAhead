using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.Tests.AnalysisDrafts;

public class DiscardAnalysisDraftUseCaseTests
{
    private static DiscardAnalysisDraftUseCase CreateUseCase(FakeAnalysisDraftRepository repository, Guid ownerUserId) =>
        new(repository, new FakeUnitOfWork(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    [Fact]
    public async Task ExecuteAsync_ForAPendingDraft_DiscardsItAndReturnsDiscarded()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var outcome = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal(DiscardAnalysisDraftOutcome.Discarded, outcome);
        Assert.Equal(AnalysisDraftStatus.Discarded, draft.Status);
        Assert.NotNull(draft.DiscardedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnEmptyPendingDraft_StillDiscardsCleanly()
    {
        // The zero-proposal case Apply can also resolve trivially, but Discard must work
        // unconditionally regardless of proposal count.
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var outcome = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal(DiscardAnalysisDraftOutcome.Discarded, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSuchDraft_ReturnsDraftNotFound()
    {
        var useCase = CreateUseCase(new FakeAnalysisDraftRepository(), Guid.NewGuid());

        var outcome = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(DiscardAnalysisDraftOutcome.DraftNotFound, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnAlreadyAppliedDraft_ReturnsDraftNotPending_AndDoesNotChangeStatus()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        draft.MarkApplied(DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var outcome = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal(DiscardAnalysisDraftOutcome.DraftNotPending, outcome);
        Assert.Equal(AnalysisDraftStatus.Applied, draft.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnotherOwnersDraft_ReturnsDraftNotFound()
    {
        var repository = new FakeAnalysisDraftRepository();
        var draft = new AnalysisDraft(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, Guid.NewGuid());

        var outcome = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal(DiscardAnalysisDraftOutcome.DraftNotFound, outcome);
    }
}
