namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// Discriminated union for a SuggestionProposal's payload (ADR-0005) — either a typed command
/// (<see cref="StructuredSuggestion"/>) or free-form text requiring manual follow-up
/// (<see cref="AdvisorySuggestion"/>).
/// </summary>
public abstract class SuggestionPayload
{
}
