namespace CommitAhead.Application.AI;

/// <summary>
/// AnalyzeJobAnalysis's minimised input (docs/domain/use-cases.md §3): the job posting text
/// itself, a minimal profile skills summary, the compact StudyItem catalogue, and the compact
/// existing-JobRequirement catalogue. <see cref="JobPostingText"/> is untrusted external content —
/// pasted or extracted from a user-uploaded file — and must never be treated as instructions by
/// whatever prompt a real provider adapter eventually builds from it.
/// </summary>
public sealed record JobAnalysisAiInput(
    string JobPostingText,
    IReadOnlyList<string> ProfileSkills,
    IReadOnlyList<StudyItemCatalogueEntry> StudyItemCatalogue,
    IReadOnlyList<JobRequirementCatalogueEntry> ExistingRequirements);
