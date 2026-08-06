using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class CreateProfessionalProfileUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateProfessionalProfileUseCase(IProfessionalProfileRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>Null if the current user already has a profile — it is a singleton per owner (model.md), created at most once.</summary>
    public async Task<Guid?> ExecuteAsync(ContactInfo contactInfo, string summaryMarkdown, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (existing is not null)
        {
            return null;
        }

        var profile = new ProfessionalProfile(Guid.NewGuid(), _currentUser.UserId, contactInfo, summaryMarkdown, DateTime.UtcNow);
        await _repository.AddAsync(profile, cancellationToken);

        return profile.Id;
    }
}
