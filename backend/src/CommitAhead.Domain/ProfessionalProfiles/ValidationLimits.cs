namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>
/// Every length/count ceiling referenced by ProfessionalProfile domain validation. A local copy,
/// not shared with <c>CommitAhead.Domain.StudyItems.ValidationLimits</c> — Phase 1 established
/// per-aggregate copies rather than a shared kernel, and this slice follows that precedent.
/// </summary>
public static class ValidationLimits
{
    public const int ShortTextMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int MarkdownMaxLength = 20_000;
    public const int ListEntryMaxLength = 500;
    public const int MaxListEntryCount = 50;
    public const int UrlMaxLength = 2000;
}
