using CommitAhead.Application.Identity;

namespace CommitAhead.Application.StudyItems;

public enum DeleteStudyItemResult
{
    Success,
    NotFound,

    /// <summary>Invariant 2 — blocked while any StudyReview or EvidenceLink still references the item.</summary>
    Blocked,
}

public sealed class DeleteStudyItemUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly IEvidenceLinkQuery _evidenceLinkQuery;
    private readonly ICurrentUser _currentUser;

    public DeleteStudyItemUseCase(IStudyItemRepository repository, IEvidenceLinkQuery evidenceLinkQuery, ICurrentUser currentUser)
    {
        _repository = repository;
        _evidenceLinkQuery = evidenceLinkQuery;
        _currentUser = currentUser;
    }

    public async Task<DeleteStudyItemResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return DeleteStudyItemResult.NotFound;
        }

        if (!item.CanBeHardDeleted || await _evidenceLinkQuery.AnyTargetingStudyItemAsync(_currentUser.UserId, id, cancellationToken))
        {
            return DeleteStudyItemResult.Blocked;
        }

        await _repository.DeleteAsync(item, cancellationToken);

        return DeleteStudyItemResult.Success;
    }
}
