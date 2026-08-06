using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>Shared by ExperienceEntry and ProjectEntry — the only two entities that carry a SkillIds list.</summary>
internal static class SkillReferenceValidation
{
    public static IReadOnlyList<Guid> ValidateSkillIds(IEnumerable<Guid> skillIds, string paramName)
    {
        var list = skillIds.ToList();
        if (list.Count > ValidationLimits.MaxListEntryCount)
        {
            throw new DomainValidationException($"{paramName} must have at most {ValidationLimits.MaxListEntryCount} entries.");
        }

        var seen = new HashSet<Guid>();
        foreach (var skillId in list)
        {
            if (skillId == Guid.Empty)
            {
                throw new DomainValidationException($"{paramName} entries must not be empty.");
            }

            if (!seen.Add(skillId))
            {
                throw new DomainValidationException($"{paramName} must not contain duplicate entries.");
            }
        }

        return list;
    }
}
