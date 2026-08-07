using CommitAhead.Application.Identity;

namespace CommitAhead.Application.InterviewNotes;

/// <summary>
/// Unconditional hard delete. No EvidenceLink/AnalysisDraft cleanup (ADR-0011): moved to Phase 4
/// (docs/roadmap.md) — EvidenceLink has no creation path and AnalysisDraft doesn't exist at all
/// until then, so there is nothing a note deletion could need to clean up yet. Same treatment
/// already given to DeleteCVPresentationUseCase in Phase 2 and DeleteJobAnalysisUseCase above.
/// </summary>
public sealed class DeleteInterviewNoteUseCase
{
    private readonly IInterviewNoteRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DeleteInterviewNoteUseCase(IInterviewNoteRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<InterviewNoteMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (note is null)
        {
            return InterviewNoteMutationResult.NotFound;
        }

        await _repository.DeleteAsync(note, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return InterviewNoteMutationResult.Success;
    }
}
