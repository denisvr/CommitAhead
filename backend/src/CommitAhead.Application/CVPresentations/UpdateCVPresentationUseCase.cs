using CommitAhead.Application.Identity;

namespace CommitAhead.Application.CVPresentations;

public sealed class UpdateCVPresentationUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateCVPresentationUseCase(ICVPresentationRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<CVPresentationMutationResult> ExecuteAsync(
        Guid id,
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
        var presentation = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (presentation is null)
        {
            return CVPresentationMutationResult.NotFound;
        }

        presentation.Update(
            label, targetMarket, targetRole, locale, templateKey, summaryOverrideMarkdown,
            includePhoto, includeEmail, includePhone, includeAddress, dateFormat, pageLimit, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return CVPresentationMutationResult.Success;
    }
}
