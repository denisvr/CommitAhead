using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical employment record inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class ExperienceEntry
{
    public Guid Id { get; }
    public string Company { get; }
    public string? Client { get; }
    public string Role { get; }
    public EmploymentType EmploymentType { get; }
    public YearMonth StartDate { get; }
    public YearMonth? EndDate { get; }
    public string? Location { get; }
    public WorkMode WorkMode { get; }
    public string SummaryMarkdown { get; }
    public IReadOnlyList<string> Achievements { get; }
    public IReadOnlyList<Guid> SkillIds { get; }

    public ExperienceEntry(
        Guid id,
        string company,
        string? client,
        string role,
        EmploymentType employmentType,
        YearMonth startDate,
        YearMonth? endDate,
        string? location,
        WorkMode workMode,
        string summaryMarkdown,
        IEnumerable<string> achievements,
        IEnumerable<Guid> skillIds)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (!Enum.IsDefined(employmentType))
        {
            throw new DomainValidationException("EmploymentType is not a recognized value.");
        }

        if (!Enum.IsDefined(workMode))
        {
            throw new DomainValidationException("WorkMode is not a recognized value.");
        }

        Id = id;
        Company = TextValidation.RequireNonBlank(company, nameof(company), ValidationLimits.ShortTextMaxLength);
        Client = TextValidation.TrimToNullOrValidate(client, nameof(client), ValidationLimits.ShortTextMaxLength);
        Role = TextValidation.RequireNonBlank(role, nameof(role), ValidationLimits.ShortTextMaxLength);
        EmploymentType = employmentType;
        StartDate = startDate;
        EndDate = endDate;
        Location = TextValidation.TrimToNullOrValidate(location, nameof(location), ValidationLimits.ShortTextMaxLength);
        WorkMode = workMode;
        SummaryMarkdown = TextValidation.RequireNonBlank(summaryMarkdown, nameof(summaryMarkdown), ValidationLimits.MarkdownMaxLength);
        Achievements = TextValidation.RequireEntries(achievements, nameof(achievements));
        SkillIds = SkillReferenceValidation.ValidateSkillIds(skillIds, nameof(skillIds));
    }
}
