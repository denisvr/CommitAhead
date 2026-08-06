using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceProjectsUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ReplaceProjectsUseCase(IProfessionalProfileRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<ProjectEntry> projects, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        profile.ReplaceProjects(projects, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
