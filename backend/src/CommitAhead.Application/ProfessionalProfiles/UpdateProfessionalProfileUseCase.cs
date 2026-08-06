using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class UpdateProfessionalProfileUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateProfessionalProfileUseCase(IProfessionalProfileRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ProfessionalProfileMutationResult> ExecuteAsync(ContactInfo contactInfo, string summaryMarkdown, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            return ProfessionalProfileMutationResult.NotFound;
        }

        var updatedAtUtc = DateTime.UtcNow;
        profile.UpdateContactInfo(contactInfo, updatedAtUtc);
        profile.UpdateSummary(summaryMarkdown, updatedAtUtc);
        await _repository.SaveChangesAsync(cancellationToken);

        return ProfessionalProfileMutationResult.Success;
    }
}
