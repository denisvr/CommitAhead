namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// The fully-resolved projection ExportCVPresentationUseCase hands to <see cref="IExportRenderer"/>
/// (ADR-0020) — every selection already resolved in order against the ProfessionalProfile, every
/// visibility flag already applied, every date already locale-formatted, every Markdown field
/// already sanitised (<see cref="RestrictedMarkdownParser"/>). The renderer owns layout only; it
/// never re-derives a business rule from raw domain data.
/// </summary>
public sealed record CVExportDocument(
    string Label,
    string TargetMarket,
    string? TargetRole,
    int PageLimit,
    CVExportContact Contact,
    IReadOnlyList<MarkdownBlock> Summary,
    IReadOnlyList<CVExportExperience> Experience,
    IReadOnlyList<CVExportEducation> Education,
    IReadOnlyList<string> Skills,
    IReadOnlyList<CVExportLanguage> Languages,
    IReadOnlyList<CVExportCertification> Certifications,
    IReadOnlyList<CVExportProject> Projects,
    IReadOnlyList<CVExportLink> ProfileLinks);

/// <summary>Email/Phone/Address are null when the presentation's own IncludeX flag excludes them. Photo is not yet supported — no upload/storage path exists for ContactInfo.PhotoStorageKey anywhere in this codebase (Phase 5's first template omits it regardless of IncludePhoto).</summary>
public sealed record CVExportContact(string Name, string? Email, string? Phone, string? Address);

public sealed record CVExportExperience(
    string Company,
    string? Client,
    string Role,
    string EmploymentType,
    string WorkMode,
    string? Location,
    string DateRange,
    IReadOnlyList<MarkdownBlock> Summary,
    IReadOnlyList<string> Achievements);

public sealed record CVExportEducation(
    string Institution,
    string Degree,
    string? Field,
    string? Location,
    string DateRange,
    IReadOnlyList<MarkdownBlock> Details);

public sealed record CVExportLanguage(string Language, string Proficiency, string? Certification);

public sealed record CVExportCertification(
    string Name,
    string IssuingOrganisation,
    string? IssuedAt,
    string? ExpiresAt,
    string? CredentialId,
    string? Url);

public sealed record CVExportProject(
    string Name,
    string? Role,
    string DateRange,
    IReadOnlyList<MarkdownBlock> Description,
    string? Url);

public sealed record CVExportLink(string Label, string Url);

/// <summary>
/// The renderer's own PDF bytes plus the actual page count it produced. Counting pages is a
/// PDF-library concern (Infrastructure, via PdfPig internally) — keeping it out of this record's
/// producer contract is what lets Application stay free of any PDF-reading dependency.
/// </summary>
public sealed record RenderedCVExport(byte[] PdfBytes, int PageCount);

/// <summary>
/// Layout-only port (ADR-0020) — the real implementation (Infrastructure: QuestPdfCVExportRenderer)
/// has no business logic of its own, only page composition. The use case compares PageCount
/// against PageLimit itself; it never reads PDF bytes.
/// </summary>
public interface IExportRenderer
{
    RenderedCVExport Render(CVExportDocument document);
}
