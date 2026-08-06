using CommitAhead.Application.Identity;

namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// Unconditional hard delete — unlike StudyItem, nothing guards CVPresentation deletion (ADR-0012).
/// Polymorphic-source cleanup (ADR-0011, if this presentation has ever been an EvidenceLink/
/// AnalysisDraft source) is explicitly Phase 4 work per docs/roadmap.md's own Phase 4 checklist —
/// not implemented here.
/// </summary>
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
        var presentation = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (presentation is null)
        {
            return CVPresentationMutationResult.NotFound;
        }

        await _repository.DeleteAsync(presentation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
