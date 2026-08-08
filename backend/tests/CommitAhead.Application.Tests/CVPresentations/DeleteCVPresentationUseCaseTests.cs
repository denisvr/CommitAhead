using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.AnalysisDrafts;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.Tests.CVPresentations;

public class DeleteCVPresentationUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    private static DeleteCVPresentationUseCase CreateUseCase(
        FakeCVPresentationRepository repository, Guid ownerUserId, FakeEvidenceLinkRepository? evidenceLinkRepository = null, FakeAnalysisDraftRepository? analysisDraftRepository = null) =>
        new(
            repository, evidenceLinkRepository ?? new FakeEvidenceLinkRepository(), analysisDraftRepository ?? new FakeAnalysisDraftRepository(), new FakeUnitOfWork(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    [Fact]
    public async Task ExecuteAsync_WithAnExistingPresentation_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId);
        await repository.AddAsync(presentation, CancellationToken.None);
        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Empty(repository.Presentations);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var repository = new FakeCVPresentationRepository();
        var useCase = CreateUseCase(repository, Guid.NewGuid());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }

    /// <summary>ADR-0011: deleting the source must also remove its EvidenceLinks and AnalysisDrafts, leaving unrelated ones untouched.</summary>
    [Fact]
    public async Task ExecuteAsync_DeletesEvidenceLinksAndAnalysisDraftsForThisSource_ButLeavesOthersUntouched()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId);
        await repository.AddAsync(presentation, CancellationToken.None);

        var otherSourceId = Guid.NewGuid();
        var evidenceLinkRepository = new FakeEvidenceLinkRepository();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, presentation.Id, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow), CancellationToken.None);
        var otherSourceLink = new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, otherSourceId, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow);
        await evidenceLinkRepository.AddAsync(otherSourceLink, CancellationToken.None);

        var analysisDraftRepository = new FakeAnalysisDraftRepository();
        await analysisDraftRepository.AddAsync(
            new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, presentation.Id, [], [], [], DateTime.UtcNow), CancellationToken.None);
        var otherSourceDraft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, otherSourceId, [], [], [], DateTime.UtcNow);
        await analysisDraftRepository.AddAsync(otherSourceDraft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId, evidenceLinkRepository, analysisDraftRepository);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal([otherSourceLink], evidenceLinkRepository.Links);
        Assert.Equal([otherSourceDraft], analysisDraftRepository.Drafts);
    }
}
