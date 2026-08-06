using CommitAhead.Application.Identity;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain;

namespace CommitAhead.Application.CVPresentations;

public sealed class ReplaceExperienceSelectionsUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly IProfessionalProfileRepository _professionalProfileRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceExperienceSelectionsUseCase(ICVPresentationRepository repository, IProfessionalProfileRepository professionalProfileRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _professionalProfileRepository = professionalProfileRepository;
        _currentUser = currentUser;
    }

    /// <summary>Invariant 23 (application-enforced per ADR-0012) is checked here: every entryId must exist in the referenced ProfessionalProfile's own Experience collection.</summary>
    public async Task<CVPresentationMutationResult> ExecuteAsync(Guid id, IEnumerable<Guid> entryIds, CancellationToken cancellationToken)
    {
        var presentation = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (presentation is null)
        {
            return CVPresentationMutationResult.NotFound;
        }

        var profile = await _professionalProfileRepository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        var validEntryIds = profile?.Experience.Select(entry => entry.Id).ToHashSet() ?? [];
        var candidateIds = entryIds.ToList();
        if (candidateIds.Any(entryId => !validEntryIds.Contains(entryId)))
        {
            throw new DomainValidationException("entryIds references an Experience entry that does not exist in the referenced ProfessionalProfile.");
        }

        presentation.ReplaceExperienceSelections(candidateIds, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
