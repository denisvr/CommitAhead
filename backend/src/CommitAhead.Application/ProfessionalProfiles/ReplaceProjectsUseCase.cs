using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceProjectsUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceProjectsUseCase(IProfessionalProfileRepository repository, ICVPresentationRepository cvPresentationRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _cvPresentationRepository = cvPresentationRepository;
        _currentUser = currentUser;
    }

    /// <summary>Also cleans up any CVPresentation selection referencing an entry removed by this replace (invariant 25) — see DanglingSelectionCleanup.</summary>
    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<ProjectEntry> projects, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        var newEntries = projects.ToList();
        var removedIds = profile.Projects.Select(entry => entry.Id).Except(newEntries.Select(entry => entry.Id)).ToHashSet();

        profile.ReplaceProjects(newEntries, DateTime.UtcNow);

        await DanglingSelectionCleanup.RemoveDanglingSelectionsAsync(
            _cvPresentationRepository,
            _currentUser.UserId,
            removedIds,
            presentation => presentation.ProjectSelections,
            (presentation, ids, updatedAtUtc) => presentation.ReplaceProjectSelections(ids, updatedAtUtc),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
