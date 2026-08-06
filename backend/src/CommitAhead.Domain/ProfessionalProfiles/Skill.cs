using System.Text.RegularExpressions;
using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical skill inside a ProfessionalProfile (CONTEXT.md). NormalizedKey is derived from DisplayName, not caller-supplied.</summary>
public sealed partial class Skill
{
    public Guid Id { get; }
    public string DisplayName { get; }
    public string NormalizedKey { get; }
    public SkillCategory Category { get; }
    public SkillProficiency? Proficiency { get; }

    public Skill(Guid id, string displayName, SkillCategory category, SkillProficiency? proficiency)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new DomainValidationException("Category is not a recognized value.");
        }

        if (proficiency is not null && !Enum.IsDefined(proficiency.Value))
        {
            throw new DomainValidationException("Proficiency is not a recognized value.");
        }

        Id = id;
        DisplayName = TextValidation.RequireNonBlank(displayName, nameof(displayName), ValidationLimits.ShortTextMaxLength);
        NormalizedKey = Normalize(DisplayName);
        Category = category;
        Proficiency = proficiency;
    }

    private static string Normalize(string displayName)
    {
        var lowered = displayName.Trim().ToLowerInvariant();
        var kebab = NonAlphanumericRun().Replace(lowered, "-");
        return kebab.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}
