using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class DeleteCVPresentationUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingPresentation_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId);
        await repository.AddAsync(presentation, CancellationToken.None);
        var useCase = new DeleteCVPresentationUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Empty(repository.Presentations);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var repository = new FakeCVPresentationRepository();
        var useCase = new DeleteCVPresentationUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }
}
