using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>
/// The canonical record of a user's professional identity — a singleton per user (ADR-0015), not
/// a single global record (CONTEXT.md). Holds seven child collections (Experience, Education,
/// Skill, Language, Certification, Project, ProfileLink) with no domain ordering invariant of
/// their own — only CVPresentation's selections are explicitly ordered (see docs/domain/model.md).
///
/// Every Replace* method takes the caller's full candidate collection and replaces it wholesale
/// (an editor-form shape, not incremental add/remove) — the caller (the future Application use
/// case) is responsible for building the complete desired collection before calling. Each Replace*
/// validates the entire candidate state and only then assigns and touches UpdatedAtUtc: a failed
/// validation leaves the aggregate exactly as it was.
/// </summary>
public sealed class ProfessionalProfile
{
    private List<ExperienceEntry> _experience = [];
    private List<EducationEntry> _education = [];
    private List<Skill> _skills = [];
    private List<LanguageEntry> _languages = [];
    private List<CertificationEntry> _certifications = [];
    private List<ProjectEntry> _projects = [];
    private List<ProfileLink> _profileLinks = [];

    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public ContactInfo ContactInfo { get; private set; }
    public string SummaryMarkdown { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<ExperienceEntry> Experience => _experience;
    public IReadOnlyList<EducationEntry> Education => _education;
    public IReadOnlyList<Skill> Skills => _skills;
    public IReadOnlyList<LanguageEntry> Languages => _languages;
    public IReadOnlyList<CertificationEntry> Certifications => _certifications;
    public IReadOnlyList<ProjectEntry> Projects => _projects;
    public IReadOnlyList<ProfileLink> ProfileLinks => _profileLinks;

    public ProfessionalProfile(Guid id, Guid ownerUserId, ContactInfo contactInfo, string summaryMarkdown, DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        ContactInfo = contactInfo;
        SummaryMarkdown = TextValidation.RequireNonBlank(summaryMarkdown, nameof(summaryMarkdown), ValidationLimits.MarkdownMaxLength);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void UpdateContactInfo(ContactInfo contactInfo, DateTime updatedAtUtc)
    {
        ContactInfo = contactInfo;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateSummary(string summaryMarkdown, DateTime updatedAtUtc)
    {
        SummaryMarkdown = TextValidation.RequireNonBlank(summaryMarkdown, nameof(summaryMarkdown), ValidationLimits.MarkdownMaxLength);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceEducation(IEnumerable<EducationEntry> education, DateTime updatedAtUtc)
    {
        _education = ValidateCollection(education, entry => entry.Id, nameof(education));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceLanguages(IEnumerable<LanguageEntry> languages, DateTime updatedAtUtc)
    {
        _languages = ValidateCollection(languages, entry => entry.Id, nameof(languages));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceCertifications(IEnumerable<CertificationEntry> certifications, DateTime updatedAtUtc)
    {
        _certifications = ValidateCollection(certifications, entry => entry.Id, nameof(certifications));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceProfileLinks(IEnumerable<ProfileLink> profileLinks, DateTime updatedAtUtc)
    {
        _profileLinks = ValidateCollection(profileLinks, entry => entry.Id, nameof(profileLinks));
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Validates every candidate entry's SkillIds against the CURRENT Skills before assigning anything (invariant 21).</summary>
    public void ReplaceExperience(IEnumerable<ExperienceEntry> experience, DateTime updatedAtUtc)
    {
        var validated = ValidateCollection(experience, entry => entry.Id, nameof(experience));
        ValidateSkillReferencesExist(validated.SelectMany(entry => entry.SkillIds), nameof(experience));

        _experience = validated;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Same shape as <see cref="ReplaceExperience"/> — Project is the other entity that references Skill.</summary>
    public void ReplaceProjects(IEnumerable<ProjectEntry> projects, DateTime updatedAtUtc)
    {
        var validated = ValidateCollection(projects, entry => entry.Id, nameof(projects));
        ValidateSkillReferencesExist(validated.SelectMany(entry => entry.SkillIds), nameof(projects));

        _projects = validated;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Validates the complete candidate state — NormalizedKey uniqueness (invariant 20) and that no
    /// Skill still referenced by a current Experience/Project entry would be removed (invariant 22)
    /// — before assigning anything, so a rejected replacement leaves Skills, Experience, and
    /// Projects all unchanged.
    /// </summary>
    public void ReplaceSkills(IEnumerable<Skill> skills, DateTime updatedAtUtc)
    {
        var validated = ValidateCollection(skills, skill => skill.Id, nameof(skills));

        var normalizedKeys = new HashSet<string>();
        foreach (var skill in validated)
        {
            if (!normalizedKeys.Add(skill.NormalizedKey))
            {
                throw new DomainValidationException("Skills must have unique NormalizedKey values.");
            }
        }

        var remainingSkillIds = validated.Select(skill => skill.Id).ToHashSet();
        var referencedSkillIds = _experience.SelectMany(entry => entry.SkillIds).Concat(_projects.SelectMany(entry => entry.SkillIds));
        if (referencedSkillIds.Any(skillId => !remainingSkillIds.Contains(skillId)))
        {
            throw new DomainValidationException("Cannot remove a Skill that is still referenced by an Experience or Project entry.");
        }

        _skills = validated;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void ValidateSkillReferencesExist(IEnumerable<Guid> referencedSkillIds, string paramName)
    {
        var availableSkillIds = _skills.Select(skill => skill.Id).ToHashSet();
        if (referencedSkillIds.Any(skillId => !availableSkillIds.Contains(skillId)))
        {
            throw new DomainValidationException($"{paramName} references a Skill that does not exist in this profile.");
        }
    }

    private static List<T> ValidateCollection<T>(IEnumerable<T> candidates, Func<T, Guid> idSelector, string paramName)
    {
        var result = new List<T>();
        var seenIds = new HashSet<Guid>();
        foreach (var candidate in candidates)
        {
            if (candidate is null)
            {
                throw new DomainValidationException($"{paramName} entries must not be null.");
            }

            var id = idSelector(candidate);
            if (id == Guid.Empty)
            {
                throw new DomainValidationException($"{paramName} entries must have a non-empty Id.");
            }

            if (!seenIds.Add(id))
            {
                throw new DomainValidationException($"{paramName} must not contain duplicate Ids.");
            }

            result.Add(candidate);
        }

        return result;
    }
}
