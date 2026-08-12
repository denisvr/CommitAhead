using CommitAhead.Application.Identity;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;
using UglyToad.PdfPig;

namespace CommitAhead.Application.CVPresentations;

public enum ExportCVPresentationOutcome
{
    Exported,
    PresentationNotFound,
    PageLimitExceeded,
}

public sealed record ExportCVPresentationResult(ExportCVPresentationOutcome Outcome, byte[]? PdfBytes, int? PageCount);

/// <summary>
/// Resolves a CVPresentation into the renderer-ready <see cref="CVExportDocument"/> projection —
/// selected canonical entries in selection order, visibility flags applied, dates locale-formatted,
/// Markdown sanitised — and renders it via <see cref="IExportRenderer"/> (ADR-0020). PageLimit is a
/// hard cap: the rendered PDF is read back with PdfPig to count its actual pages, since QuestPDF's
/// own layout has no page-count constraint to enforce mid-render.
/// </summary>
public sealed class ExportCVPresentationUseCase
{
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly IProfessionalProfileRepository _profileRepository;
    private readonly IExportRenderer _renderer;
    private readonly ICurrentUser _currentUser;

    public ExportCVPresentationUseCase(
        ICVPresentationRepository cvPresentationRepository, IProfessionalProfileRepository profileRepository, IExportRenderer renderer, ICurrentUser currentUser)
    {
        _cvPresentationRepository = cvPresentationRepository;
        _profileRepository = profileRepository;
        _renderer = renderer;
        _currentUser = currentUser;
    }

    public async Task<ExportCVPresentationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;
        var presentation = await _cvPresentationRepository.GetByIdAsync(ownerUserId, id, cancellationToken);
        if (presentation is null)
        {
            return new ExportCVPresentationResult(ExportCVPresentationOutcome.PresentationNotFound, null, null);
        }

        var profile = await _profileRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("A CVPresentation must have a ProfessionalProfile.");

        var document = BuildDocument(presentation, profile);
        var pdfBytes = _renderer.Render(document);

        int pageCount;
        using (var pdf = PdfDocument.Open(pdfBytes))
        {
            pageCount = pdf.NumberOfPages;
        }

        if (pageCount > presentation.PageLimit)
        {
            return new ExportCVPresentationResult(ExportCVPresentationOutcome.PageLimitExceeded, null, pageCount);
        }

        return new ExportCVPresentationResult(ExportCVPresentationOutcome.Exported, pdfBytes, pageCount);
    }

    private static CVExportDocument BuildDocument(CVPresentation presentation, ProfessionalProfile profile)
    {
        var locale = presentation.Locale;

        var contact = new CVExportContact(
            profile.ContactInfo.Name,
            presentation.IncludeEmail ? profile.ContactInfo.Email : null,
            presentation.IncludePhone ? profile.ContactInfo.Phone : null,
            presentation.IncludeAddress ? profile.ContactInfo.Address : null);

        var summary = RestrictedMarkdownParser.Parse(presentation.SummaryOverrideMarkdown ?? profile.SummaryMarkdown);

        var experienceById = profile.Experience.ToDictionary(e => e.Id);
        var experience = presentation.ExperienceSelections
            .Where(experienceById.ContainsKey)
            .Select(id => experienceById[id])
            .Select(entry => new CVExportExperience(
                entry.Company,
                entry.Client,
                entry.Role,
                entry.EmploymentType.ToString(),
                entry.WorkMode.ToString(),
                entry.Location,
                CVExportDateFormatter.FormatDateRange(entry.StartDate, entry.EndDate, locale),
                RestrictedMarkdownParser.Parse(entry.SummaryMarkdown),
                entry.Achievements))
            .ToList();

        var educationById = profile.Education.ToDictionary(e => e.Id);
        var education = presentation.EducationSelections
            .Where(educationById.ContainsKey)
            .Select(id => educationById[id])
            .Select(entry => new CVExportEducation(
                entry.Institution,
                entry.Degree,
                entry.Field,
                entry.Location,
                CVExportDateFormatter.FormatDateRange(entry.StartDate, entry.EndDate, locale),
                RestrictedMarkdownParser.Parse(entry.DetailsMarkdown)))
            .ToList();

        var skillById = profile.Skills.ToDictionary(s => s.Id);
        var skills = presentation.SkillSelections
            .Where(skillById.ContainsKey)
            .Select(id => skillById[id].DisplayName)
            .ToList();

        var languageById = profile.Languages.ToDictionary(l => l.Id);
        var languages = presentation.LanguageSelections
            .Where(languageById.ContainsKey)
            .Select(id => languageById[id])
            .Select(entry => new CVExportLanguage(entry.Language, entry.Proficiency.ToString(), entry.Certification))
            .ToList();

        var certificationById = profile.Certifications.ToDictionary(c => c.Id);
        var certifications = presentation.CertificationSelections
            .Where(certificationById.ContainsKey)
            .Select(id => certificationById[id])
            .Select(entry => new CVExportCertification(
                entry.Name,
                entry.IssuingOrganisation,
                entry.IssuedAt is null ? null : CVExportDateFormatter.FormatYearMonth(entry.IssuedAt, locale),
                entry.ExpiresAt is null ? null : CVExportDateFormatter.FormatYearMonth(entry.ExpiresAt, locale),
                entry.CredentialId,
                entry.Url))
            .ToList();

        var projectById = profile.Projects.ToDictionary(p => p.Id);
        var projects = presentation.ProjectSelections
            .Where(projectById.ContainsKey)
            .Select(id => projectById[id])
            .Select(entry => new CVExportProject(
                entry.Name,
                entry.Role,
                CVExportDateFormatter.FormatDateRange(entry.StartDate, entry.EndDate, locale),
                RestrictedMarkdownParser.Parse(entry.DescriptionMarkdown),
                entry.Url))
            .ToList();

        var profileLinkById = profile.ProfileLinks.ToDictionary(l => l.Id);
        var profileLinks = presentation.ProfileLinkSelections
            .Where(profileLinkById.ContainsKey)
            .Select(id => profileLinkById[id])
            .Select(entry => new CVExportLink(entry.Label ?? entry.Kind.ToString(), entry.Url))
            .ToList();

        return new CVExportDocument(
            presentation.Label,
            presentation.TargetMarket,
            presentation.TargetRole,
            presentation.PageLimit,
            contact,
            summary,
            experience,
            education,
            skills,
            languages,
            certifications,
            projects,
            profileLinks);
    }
}
