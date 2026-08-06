using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class ReplaceCertificationsUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ReplaceCertificationsUseCase(IProfessionalProfileRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(IEnumerable<CertificationEntry> certifications, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        profile.ReplaceCertifications(certifications, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
