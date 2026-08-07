using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AI;

/// <summary>
/// A raw AI-proposed StudyItemProposal. DetailsJson's expected shape depends on Category, the same
/// deferred-validation approach StructuredSuggestion.PayloadJson uses — the analyzing use case
/// parses it into a real <see cref="StudyItemDetails"/> subtype and constructs the Domain
/// StudyItemProposal.
/// </summary>
public sealed record AiStudyItemProposal(string Title, StudyItemCategory Category, string DetailsJson, IReadOnlyList<string> Tags, int Importance);
