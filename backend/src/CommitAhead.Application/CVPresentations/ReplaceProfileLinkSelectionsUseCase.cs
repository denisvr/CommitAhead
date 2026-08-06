using CommitAhead.Application.Identity;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain;

namespace CommitAhead.Application.CVPresentations;

public sealed class ReplaceProfileLinkSelectionsUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly IProfessionalProfileRepository _professionalProfileRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceProfileLinkSelectionsUseCase(ICVPresentationRepository repository, IProfessionalProfileRepository professionalProfileRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _professionalProfileRepository = professionalProfileRepository;
        _currentUser = currentUser;
    }

    public async Task<CVPresentationMutationResult> ExecuteAsync(Guid id, IEnumerable<Guid> entryIds, CancellationToken cancellationToken)
    {
        var presentation = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (presentation is null)
        {
            return CVPresentationMutationResult.NotFound;
        }

        var profile = await _professionalProfileRepository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        var validEntryIds = profile?.ProfileLinks.Select(entry => entry.Id).ToHashSet() ?? [];
        var candidateIds = entryIds.ToList();
        if (candidateIds.Any(entryId => !validEntryIds.Contains(entryId)))
        {
            throw new DomainValidationException("entryIds references a ProfileLink that does not exist in the referenced ProfessionalProfile.");
        }

        presentation.ReplaceProfileLinkSelections(candidateIds, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
