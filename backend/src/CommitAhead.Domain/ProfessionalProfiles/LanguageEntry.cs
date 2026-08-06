using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical spoken-language record inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class LanguageEntry
{
    public Guid Id { get; }
    public string Language { get; }
    public LanguageProficiency Proficiency { get; }
    public string? Certification { get; }

    public LanguageEntry(Guid id, string language, LanguageProficiency proficiency, string? certification)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (!Enum.IsDefined(proficiency))
        {
            throw new DomainValidationException("Proficiency is not a recognized value.");
        }

        Id = id;
        Language = TextValidation.RequireNonBlank(language, nameof(language), ValidationLimits.ShortTextMaxLength);
        Proficiency = proficiency;
        Certification = TextValidation.TrimToNullOrValidate(certification, nameof(certification), ValidationLimits.ShortTextMaxLength);
    }
}
