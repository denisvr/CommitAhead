namespace CommitAhead.Application.AI;

/// <summary>AnalyzeInterviewNote's minimised input (docs/domain/use-cases.md §4): the structured note itself plus the compact StudyItem catalogue.</summary>
public sealed record InterviewNoteAiInput(
    string Company,
    string Role,
    string InterviewRound,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Lessons,
    IReadOnlyList<StudyItemCatalogueEntry> StudyItemCatalogue);
