using System.Globalization;
using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.CVPresentations;

/// <summary>
/// A curated, locale-specific projection over one ProfessionalProfile (CONTEXT.md) — an
/// independent aggregate root per ADR-0012, not a child of ProfessionalProfile. It never
/// duplicates canonical entry content, only selects and orders entries from each of the seven
/// canonical collections by Id.
///
/// Each selection is a plain, ordered <c>IReadOnlyList&lt;Guid&gt;</c> — list order IS position
/// (contiguous from zero by construction, invariant 24's other half), so there is no separate
/// Position field to keep in sync. Uniqueness within a selection is validated; whether each Id
/// actually exists in the referenced ProfessionalProfile (invariant 23) is application-enforced
/// per ADR-0012, since it spans two aggregates this type has no access to.
///
/// Reuses <c>CommitAhead.Domain.ProfessionalProfiles.TextValidation</c>/<c>ValidationLimits</c>
/// rather than a third per-aggregate copy — CVPresentation's fields are all short strings/markdown
/// already covered by that aggregate's limits, and it is already conceptually tied to
/// ProfessionalProfile via <see cref="ProfessionalProfileId"/> (mirrors EvidenceLink reusing
/// StudyItems' helpers).
/// </summary>
public sealed class CVPresentation
{
    private List<Guid> _experienceSelections = [];
    private List<Guid> _educationSelections = [];
    private List<Guid> _skillSelections = [];
    private List<Guid> _languageSelections = [];
    private List<Guid> _certificationSelections = [];
    private List<Guid> _projectSelections = [];
    private List<Guid> _profileLinkSelections = [];

    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public Guid ProfessionalProfileId { get; }
    public string Label { get; private set; }
    public string TargetMarket { get; private set; }
    public string? TargetRole { get; private set; }
    public string Locale { get; private set; }
    public string TemplateKey { get; private set; }
    public string? SummaryOverrideMarkdown { get; private set; }
    public bool IncludePhoto { get; private set; }
    public bool IncludeEmail { get; private set; }
    public bool IncludePhone { get; private set; }
    public bool IncludeAddress { get; private set; }
    public string DateFormat { get; private set; }
    public int PageLimit { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<Guid> ExperienceSelections => _experienceSelections;
    public IReadOnlyList<Guid> EducationSelections => _educationSelections;
    public IReadOnlyList<Guid> SkillSelections => _skillSelections;
    public IReadOnlyList<Guid> LanguageSelections => _languageSelections;
    public IReadOnlyList<Guid> CertificationSelections => _certificationSelections;
    public IReadOnlyList<Guid> ProjectSelections => _projectSelections;
    public IReadOnlyList<Guid> ProfileLinkSelections => _profileLinkSelections;

    public CVPresentation(
        Guid id,
        Guid ownerUserId,
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
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (professionalProfileId == Guid.Empty)
        {
            throw new DomainValidationException("ProfessionalProfileId is required.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        ProfessionalProfileId = professionalProfileId;
        Label = TextValidation.RequireNonBlank(label, nameof(label), ValidationLimits.ShortTextMaxLength);
        TargetMarket = TextValidation.RequireNonBlank(targetMarket, nameof(targetMarket), ValidationLimits.ShortTextMaxLength);
        TargetRole = TextValidation.TrimToNullOrValidate(targetRole, nameof(targetRole), ValidationLimits.ShortTextMaxLength);
        Locale = ValidateLocale(locale);
        TemplateKey = TextValidation.RequireNonBlank(templateKey, nameof(templateKey), ValidationLimits.ShortTextMaxLength);
        SummaryOverrideMarkdown = TextValidation.TrimToNullOrValidate(summaryOverrideMarkdown, nameof(summaryOverrideMarkdown), ValidationLimits.MarkdownMaxLength);
        IncludePhoto = includePhoto;
        IncludeEmail = includeEmail;
        IncludePhone = includePhone;
        IncludeAddress = includeAddress;
        DateFormat = TextValidation.RequireNonBlank(dateFormat, nameof(dateFormat), ValidationLimits.ShortTextMaxLength);
        PageLimit = ValidatePageLimit(pageLimit);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Replaces every mutable formatting field at once (an editor-form shape, matching
    /// StudyItem.Update) — ProfessionalProfileId is fixed at creation, a presentation is never
    /// re-pointed at a different profile. Validates everything into locals first, so a rejected
    /// update leaves every field exactly as it was.
    /// </summary>
    public void Update(
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
        DateTime updatedAtUtc)
    {
        var validatedLabel = TextValidation.RequireNonBlank(label, nameof(label), ValidationLimits.ShortTextMaxLength);
        var validatedTargetMarket = TextValidation.RequireNonBlank(targetMarket, nameof(targetMarket), ValidationLimits.ShortTextMaxLength);
        var validatedTargetRole = TextValidation.TrimToNullOrValidate(targetRole, nameof(targetRole), ValidationLimits.ShortTextMaxLength);
        var validatedLocale = ValidateLocale(locale);
        var validatedTemplateKey = TextValidation.RequireNonBlank(templateKey, nameof(templateKey), ValidationLimits.ShortTextMaxLength);
        var validatedSummaryOverrideMarkdown = TextValidation.TrimToNullOrValidate(summaryOverrideMarkdown, nameof(summaryOverrideMarkdown), ValidationLimits.MarkdownMaxLength);
        var validatedDateFormat = TextValidation.RequireNonBlank(dateFormat, nameof(dateFormat), ValidationLimits.ShortTextMaxLength);
        var validatedPageLimit = ValidatePageLimit(pageLimit);

        Label = validatedLabel;
        TargetMarket = validatedTargetMarket;
        TargetRole = validatedTargetRole;
        Locale = validatedLocale;
        TemplateKey = validatedTemplateKey;
        SummaryOverrideMarkdown = validatedSummaryOverrideMarkdown;
        IncludePhoto = includePhoto;
        IncludeEmail = includeEmail;
        IncludePhone = includePhone;
        IncludeAddress = includeAddress;
        DateFormat = validatedDateFormat;
        PageLimit = validatedPageLimit;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceExperienceSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _experienceSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceEducationSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _educationSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceSkillSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _skillSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceLanguageSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _languageSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceCertificationSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _certificationSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ReplaceProjectSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _projectSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Defaulting to every existing ProfileLink at creation (model.md) is an application-layer decision — this just replaces the collection given.</summary>
    public void ReplaceProfileLinkSelections(IEnumerable<Guid> entryIds, DateTime updatedAtUtc)
    {
        _profileLinkSelections = ValidateSelection(entryIds, nameof(entryIds));
        UpdatedAtUtc = updatedAtUtc;
    }

    // CultureInfo.GetCultureInfo(name) alone is too lenient to reject with — ICU's BCP-47 parser
    // accepts many syntactically hyphenated-but-meaningless tags (e.g. "not-a-real-locale")
    // without throwing, since it only checks tag structure, not whether the tag is a real,
    // registered culture. Checking membership in the runtime's own enumerated culture list (this
    // project does not run with InvariantGlobalization) is what actually distinguishes a real
    // BCP-47 locale like "en-GB"/"de-DE"/"pt-BR" from garbage, without hand-maintaining an
    // allowlist.
    private static readonly HashSet<string> KnownLocales = CultureInfo
        .GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
        .Select(culture => culture.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Rejects an unrecognized locale outright, rather than letting a bad value reach the preview (formatYearMonth hands it to Intl.DateTimeFormat, which throws on an unrecognized tag).</summary>
    private static string ValidateLocale(string locale)
    {
        var trimmed = TextValidation.RequireNonBlank(locale, nameof(locale), ValidationLimits.ShortTextMaxLength);

        if (!KnownLocales.Contains(trimmed))
        {
            throw new DomainValidationException($"'{trimmed}' is not a supported locale.");
        }

        return trimmed;
    }

    private static int ValidatePageLimit(int pageLimit)
    {
        if (pageLimit <= 0)
        {
            throw new DomainValidationException("PageLimit must be greater than zero.");
        }

        return pageLimit;
    }

    /// <summary>Rejects empty/duplicate entries (invariant 24's uniqueness half); list order becomes position.</summary>
    private static List<Guid> ValidateSelection(IEnumerable<Guid> entryIds, string paramName)
    {
        var result = new List<Guid>();
        var seenIds = new HashSet<Guid>();
        foreach (var entryId in entryIds)
        {
            if (entryId == Guid.Empty)
            {
                throw new DomainValidationException($"{paramName} entries must not be empty.");
            }

            if (!seenIds.Add(entryId))
            {
                throw new DomainValidationException($"{paramName} must not contain duplicate entries.");
            }

            result.Add(entryId);
        }

        return result;
    }
}
