using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceSkillsUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly ICurrentUser _currentUser;

    public ReplaceSkillsUseCase(IProfessionalProfileRepository repository, ICVPresentationRepository cvPresentationRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _cvPresentationRepository = cvPresentationRepository;
        _currentUser = currentUser;
    }

    /// <summary>Also cleans up any CVPresentation selection referencing a Skill removed by this replace (invariant 25) — see DanglingSelectionCleanup.</summary>
    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<Skill> skills, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        var newSkills = skills.ToList();
        var removedIds = profile.Skills.Select(skill => skill.Id).Except(newSkills.Select(skill => skill.Id)).ToHashSet();

        profile.ReplaceSkills(newSkills, DateTime.UtcNow);

        await DanglingSelectionCleanup.RemoveDanglingSelectionsAsync(
            _cvPresentationRepository,
            _currentUser.UserId,
            removedIds,
            presentation => presentation.SkillSelections,
            (presentation, ids, updatedAtUtc) => presentation.ReplaceSkillSelections(ids, updatedAtUtc),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
