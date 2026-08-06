using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceProfileLinksUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceProfileLinksUseCase(IProfessionalProfileRepository repository, ICVPresentationRepository cvPresentationRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _cvPresentationRepository = cvPresentationRepository;
        _currentUser = currentUser;
    }

    /// <summary>Also cleans up any CVPresentation selection referencing a ProfileLink removed by this replace (invariant 25) — see DanglingSelectionCleanup.</summary>
    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<ProfileLink> profileLinks, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        var newLinks = profileLinks.ToList();
        var removedIds = profile.ProfileLinks.Select(link => link.Id).Except(newLinks.Select(link => link.Id)).ToHashSet();

        profile.ReplaceProfileLinks(newLinks, DateTime.UtcNow);

        await DanglingSelectionCleanup.RemoveDanglingSelectionsAsync(
            _cvPresentationRepository,
            _currentUser.UserId,
            removedIds,
            presentation => presentation.ProfileLinkSelections,
            (presentation, ids, updatedAtUtc) => presentation.ReplaceProfileLinkSelections(ids, updatedAtUtc),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
