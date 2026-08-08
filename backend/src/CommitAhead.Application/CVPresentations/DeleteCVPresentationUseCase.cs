using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// Hard delete, transactional with ADR-0011's polymorphic-source cleanup: all EvidenceLinks and
/// AnalysisDrafts (any status, including Pending) for this CVPresentation are removed in the same
/// transaction as the CVPresentation itself — there is no real FK to cascade this, so the
/// application must.
/// </summary>
public sealed class DeleteCVPresentationUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly IEvidenceLinkRepository _evidenceLinkRepository;
    private readonly IAnalysisDraftRepository _analysisDraftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteCVPresentationUseCase(
        ICVPresentationRepository repository, IEvidenceLinkRepository evidenceLinkRepository, IAnalysisDraftRepository analysisDraftRepository,
        IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _evidenceLinkRepository = evidenceLinkRepository;
        _analysisDraftRepository = analysisDraftRepository;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _evidenceLinkRepository.DeleteAllForSourceAsync(ownerUserId, EvidenceSourceType.CVPresentation, id, ct);
                await _analysisDraftRepository.DeleteAllForSourceAsync(ownerUserId, EvidenceSourceType.CVPresentation, id, ct);
                await _repository.DeleteAsync(presentation, ct);
                await _repository.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
