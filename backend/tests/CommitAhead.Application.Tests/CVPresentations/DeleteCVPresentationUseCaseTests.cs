using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class DeleteCVPresentationUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    private static DeleteCVPresentationUseCase CreateUseCase(FakeCVPresentationRepository repository, Guid ownerUserId) =>
        new(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

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
}
