using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceEducationUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceEducationUseCase(IProfessionalProfileRepository repository, ICVPresentationRepository cvPresentationRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _cvPresentationRepository = cvPresentationRepository;
        _currentUser = currentUser;
    }

    /// <summary>Also cleans up any CVPresentation selection referencing an entry removed by this replace (invariant 25) — see DanglingSelectionCleanup.</summary>
    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<EducationEntry> education, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        var newEntries = education.ToList();
        var removedIds = profile.Education.Select(entry => entry.Id).Except(newEntries.Select(entry => entry.Id)).ToHashSet();

        profile.ReplaceEducation(newEntries, DateTime.UtcNow);

        await DanglingSelectionCleanup.RemoveDanglingSelectionsAsync(
            _cvPresentationRepository,
            _currentUser.UserId,
            removedIds,
            presentation => presentation.EducationSelections,
            (presentation, ids, updatedAtUtc) => presentation.ReplaceEducationSelections(ids, updatedAtUtc),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
