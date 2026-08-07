namespace CommitAhead.Application.AI;

/// <summary>AnalyzeJobAnalysis's minimised input (docs/domain/use-cases.md §3): the job posting text itself, a minimal profile skills summary, and the compact StudyItem catalogue.</summary>
public sealed record JobAnalysisAiInput(string JobPostingText, IReadOnlyList<string> ProfileSkills, IReadOnlyList<StudyItemCatalogueEntry> StudyItemCatalogue);
