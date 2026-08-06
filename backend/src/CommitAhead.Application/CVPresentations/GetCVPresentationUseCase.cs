using CommitAhead.Application.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.CVPresentations;

public sealed class GetCVPresentationUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetCVPresentationUseCase(ICVPresentationRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<CVPresentationResult?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var presentation = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        return presentation is null ? null : CVPresentationResult.FromDomain(presentation);
    }
}

public sealed record CVPresentationResult(
    Guid Id,
    Guid ProfessionalProfileId,
    string Label,
    string TargetMarket,
    string? TargetRole,
    string Locale,
    string TemplateKey,
    string? SummaryOverrideMarkdown,
    bool IncludePhoto,
    bool IncludeEmail,
    bool IncludePhone,
    bool IncludeAddress,
    string DateFormat,
    int PageLimit,
    IReadOnlyList<Guid> ExperienceSelections,
    IReadOnlyList<Guid> EducationSelections,
    IReadOnlyList<Guid> SkillSelections,
    IReadOnlyList<Guid> LanguageSelections,
    IReadOnlyList<Guid> CertificationSelections,
    IReadOnlyList<Guid> ProjectSelections,
    IReadOnlyList<Guid> ProfileLinkSelections,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static CVPresentationResult FromDomain(CVPresentation presentation) => new(
        presentation.Id,
        presentation.ProfessionalProfileId,
        presentation.Label,
        presentation.TargetMarket,
        presentation.TargetRole,
        presentation.Locale,
        presentation.TemplateKey,
        presentation.SummaryOverrideMarkdown,
        presentation.IncludePhoto,
        presentation.IncludeEmail,
        presentation.IncludePhone,
        presentation.IncludeAddress,
        presentation.DateFormat,
        presentation.PageLimit,
        presentation.ExperienceSelections,
        presentation.EducationSelections,
        presentation.SkillSelections,
        presentation.LanguageSelections,
        presentation.CertificationSelections,
        presentation.ProjectSelections,
        presentation.ProfileLinkSelections,
        presentation.CreatedAtUtc,
        presentation.UpdatedAtUtc);
}
