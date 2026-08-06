using CommitAhead.Application.Identity;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.CVPresentations;

public sealed class CreateCVPresentationUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly IProfessionalProfileRepository _professionalProfileRepository;
    private readonly ICurrentUser _currentUser;

    public CreateCVPresentationUseCase(ICVPresentationRepository repository, IProfessionalProfileRepository professionalProfileRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _professionalProfileRepository = professionalProfileRepository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Null if professionalProfileId doesn't refer to the current user's own ProfessionalProfile
    /// (invariant 29 — cross-owner reference, made trivial since GetByOwnerUserIdAsync already
    /// scopes by the current user; there is nothing else professionalProfileId could validly be).
    /// </summary>
    public async Task<Guid?> ExecuteAsync(
        Guid professionalProfileId,
        string label,
        string targetMarket,
        string? targetRole,
        string locale,
        string templateKey,
        string? summaryOverrideMarkdown,
        bool includePhoto,
        bool includeEmail,
        bool includePhone,
        bool includeAddress,
        string dateFormat,
        int pageLimit,
        CancellationToken cancellationToken)
    {
        var profile = await _professionalProfileRepository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        if (profile is null || profile.Id != professionalProfileId)
        {
            return null;
        }

        var presentation = new CVPresentation(
            Guid.NewGuid(),
            _currentUser.UserId,
            professionalProfileId,
            label,
            targetMarket,
            targetRole,
            locale,
            templateKey,
            summaryOverrideMarkdown,
            includePhoto,
            includeEmail,
            includePhone,
            includeAddress,
            dateFormat,
            pageLimit,
            DateTime.UtcNow);

        // model.md: ProfileLinks default to every existing link when a presentation is created.
        presentation.ReplaceProfileLinkSelections(profile.ProfileLinks.Select(link => link.Id), DateTime.UtcNow);

        await _repository.AddAsync(presentation, cancellationToken);

        return presentation.Id;
    }
}
