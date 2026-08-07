namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// The StructuredSuggestion command allowlist (docs/tbd.md's "StructuredSuggestion command
/// allowlist" entry, resolved at Phase 4 kickoff to exactly its "minimum candidates" list — no
/// commands added beyond it). A source mutation not named here can only ever be proposed as an
/// AdvisorySuggestion, never applied automatically.
/// </summary>
public enum StructuredSuggestionCommandType
{
    AddJobRequirement,
    AddJobGap,
    UpdateCVPresentationSummary,
    AddInterviewGap,
    AddInterviewLesson,
}
