using CommitAhead.Application.Identity;

namespace CommitAhead.Application.CVPresentations;

/// <summary>Hard delete — a single aggregate, no cross-aggregate cleanup needed.</summary>
public sealed class DeleteCVPresentationUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DeleteCVPresentationUseCase(ICVPresentationRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<CVPresentationMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;
        var presentation = await _repository.GetByIdAsync(ownerUserId, id, cancellationToken);
        if (presentation is null)
        {
            return CVPresentationMutationResult.NotFound;
        }

        await _repository.DeleteAsync(presentation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
