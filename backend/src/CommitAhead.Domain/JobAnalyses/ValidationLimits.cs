namespace CommitAhead.Domain.JobAnalyses;

/// <summary>
/// Every length/count ceiling referenced by JobAnalysis-family domain validation. A local copy,
/// not shared with <c>CommitAhead.Domain.StudyItems.ValidationLimits</c> or
/// <c>CommitAhead.Domain.ProfessionalProfiles.ValidationLimits</c> — same precedent as those two
/// already not sharing one.
/// </summary>
public static class ValidationLimits
{
    public const int TitleMaxLength = 200;

    /// <summary>Shared by PastedText.Content and UploadedFile.ExtractedText — both represent "the job posting text" (ADR-0002), and 50,000 is the already-decided cap on extracted text (ADR-0010).</summary>
    public const int JobSourceTextMaxLength = 50_000;

    public const int ShortTextMaxLength = 200;
    public const int RequirementTextMaxLength = 500;
    public const int SourceExcerptMaxLength = 1_000;
    public const int GapRationaleMaxLength = 1_000;
    public const int NotesMarkdownMaxLength = 20_000;
}
