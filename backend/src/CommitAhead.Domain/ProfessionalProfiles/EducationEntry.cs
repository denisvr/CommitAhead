using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical academic record inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class EducationEntry
{
    public Guid Id { get; }
    public string Institution { get; }
    public string Degree { get; }
    public string? Field { get; }
    public YearMonth? StartDate { get; }
    public YearMonth? EndDate { get; }
    public string? Location { get; }
    public string? DetailsMarkdown { get; }

    // Assigned by ProfessionalProfile.ReplaceEducation from the caller's array order — not a
    // constructor parameter, since it's aggregate-managed persistence state, not something a
    // caller building one entry in isolation should have to supply or could get wrong.
    public int Position { get; internal set; }

    public EducationEntry(
        Guid id,
        string institution,
        string degree,
        string? field,
        YearMonth? startDate,
        YearMonth? endDate,
        string? location,
        string? detailsMarkdown)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        Id = id;
        Institution = TextValidation.RequireNonBlank(institution, nameof(institution), ValidationLimits.ShortTextMaxLength);
        Degree = TextValidation.RequireNonBlank(degree, nameof(degree), ValidationLimits.ShortTextMaxLength);
        Field = TextValidation.TrimToNullOrValidate(field, nameof(field), ValidationLimits.ShortTextMaxLength);
        StartDate = startDate;
        EndDate = endDate;
        Location = TextValidation.TrimToNullOrValidate(location, nameof(location), ValidationLimits.ShortTextMaxLength);
        DetailsMarkdown = TextValidation.TrimToNullOrValidate(detailsMarkdown, nameof(detailsMarkdown), ValidationLimits.MarkdownMaxLength);
    }
}
