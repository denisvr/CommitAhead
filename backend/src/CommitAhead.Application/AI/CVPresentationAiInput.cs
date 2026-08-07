namespace CommitAhead.Application.AI;

/// <summary>
/// AnalyzeCVPresentation's minimised input (docs/domain/use-cases.md §5): the selected canonical
/// entries resolved into short, flattened highlight strings plus the compact StudyItem catalogue.
/// Contact values, photo keys, reviews, private notes, and hidden solutions are excluded by the
/// resolving use case before this type is ever constructed — never present here to exclude later.
/// Exact highlight formatting is that resolving use case's concern (a later slice); this contract
/// only fixes the shape IAIProvider itself depends on.
/// </summary>
public sealed record CVPresentationAiInput(
    string? SummaryMarkdown,
    IReadOnlyList<string> ExperienceHighlights,
    IReadOnlyList<string> EducationHighlights,
    IReadOnlyList<string> SkillNames,
    IReadOnlyList<StudyItemCatalogueEntry> StudyItemCatalogue);
