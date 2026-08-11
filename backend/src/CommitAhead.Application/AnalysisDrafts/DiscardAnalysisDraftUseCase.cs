using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>
/// Explicitly discards one Pending AnalysisDraft (page-patterns.md "AI analysis draft": "Discard is
/// explicit") — the domain-level counterpart to Apply for a draft the user does not want to act on,
/// including an empty one (zero proposals), which Apply can also resolve trivially but which this
/// gives an explicit, unambiguous action for regardless of proposal count.
/// </summary>
public sealed class DiscardAnalysisDraftUseCase
{
    private readonly IAnalysisDraftRepository _draftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DiscardAnalysisDraftUseCase(IAnalysisDraftRepository draftRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _draftRepository = draftRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public Task<DiscardAnalysisDraftOutcome> ExecuteAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;
        return _unitOfWork.ExecuteInTransactionAsync(ct => DiscardWithinTransactionAsync(ownerUserId, draftId, ct), cancellationToken);
    }

    // Row-locks the draft (same guard ApplyAnalysisDraftUseCase uses) so a concurrent Apply/Discard
    // of the same draft can't both succeed.
    private async Task<DiscardAnalysisDraftOutcome> DiscardWithinTransactionAsync(Guid ownerUserId, Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdForUpdateAsync(ownerUserId, draftId, cancellationToken);
        if (draft is null)
        {
            return DiscardAnalysisDraftOutcome.DraftNotFound;
        }

        if (draft.Status != AnalysisDraftStatus.Pending)
        {
            return DiscardAnalysisDraftOutcome.DraftNotPending;
        }

        draft.Discard(DateTime.UtcNow);
        await _draftRepository.SaveChangesAsync(cancellationToken);

        return DiscardAnalysisDraftOutcome.Discarded;
    }
}

public enum DiscardAnalysisDraftOutcome
{
    Discarded,
    DraftNotFound,
    DraftNotPending,
}
