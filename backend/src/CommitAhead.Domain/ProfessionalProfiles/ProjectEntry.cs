using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical project record inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class ProjectEntry
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Role { get; }
    public YearMonth? StartDate { get; }
    public YearMonth? EndDate { get; }
    public string DescriptionMarkdown { get; }
    public string? Url { get; }
    public IReadOnlyList<Guid> SkillIds { get; }

    public ProjectEntry(
        Guid id,
        string name,
        string? role,
        YearMonth? startDate,
        YearMonth? endDate,
        string descriptionMarkdown,
        string? url,
        IReadOnlyList<Guid> skillIds)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        Id = id;
        Name = TextValidation.RequireNonBlank(name, nameof(name), ValidationLimits.ShortTextMaxLength);
        Role = TextValidation.TrimToNullOrValidate(role, nameof(role), ValidationLimits.ShortTextMaxLength);
        StartDate = startDate;
        EndDate = endDate;
        DescriptionMarkdown = TextValidation.RequireNonBlank(descriptionMarkdown, nameof(descriptionMarkdown), ValidationLimits.MarkdownMaxLength);
        Url = TextValidation.ValidateOptionalAbsoluteUrl(url, nameof(url), "http", "https");
        SkillIds = SkillReferenceValidation.ValidateSkillIds(skillIds, nameof(skillIds));
    }
}
