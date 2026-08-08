using CommitAhead.Application.Identity;

namespace CommitAhead.Application.EvidenceLinks;

/// <summary>
/// Unconditional hard delete — per docs/domain/model.md, an EvidenceLink has no proposal lifecycle
/// ("existence means active; deletion removes their contribution to Demand"), and nothing else
/// references a specific EvidenceLink, so no other cleanup is needed here.
/// </summary>
public sealed class DeleteEvidenceLinkUseCase
{
    private readonly IEvidenceLinkRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DeleteEvidenceLinkUseCase(IEvidenceLinkRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<EvidenceLinkMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var link = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (link is null)
        {
            return EvidenceLinkMutationResult.NotFound;
        }

        await _repository.DeleteAsync(link, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return EvidenceLinkMutationResult.Success;
    }
}
