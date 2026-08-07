namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// Every length ceiling referenced by AnalysisDraft-family domain validation. A local copy, not
/// shared with other aggregates' <c>ValidationLimits</c> — same precedent as JobAnalyses' and
/// StudyItems' own copies.
/// </summary>
public static class ValidationLimits
{
    public const int AdvisoryMarkdownMaxLength = 5_000;
    public const int StructuredSuggestionPayloadJsonMaxLength = 5_000;
    public const int LinkProposalRationaleMaxLength = 1_000;
}
