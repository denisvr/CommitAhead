using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class UpdateCVPresentationUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingPresentation_UpdatesItAndReturnsSuccess()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId);
        await repository.AddAsync(presentation, CancellationToken.None);
        var useCase = new UpdateCVPresentationUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, "New label", "Germany", "Backend Engineer", "de-DE", "classic", "Override", true, false, false, true, "yyyy-MM-dd", 3, CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.Success, result);
        Assert.Equal("New label", presentation.Label);
        Assert.Equal(3, presentation.PageLimit);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPresentation_ReturnsNotFound()
    {
        var repository = new FakeCVPresentationRepository();
        var useCase = new UpdateCVPresentationUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, CancellationToken.None);

        Assert.Equal(CVPresentationMutationResult.NotFound, result);
    }
}
