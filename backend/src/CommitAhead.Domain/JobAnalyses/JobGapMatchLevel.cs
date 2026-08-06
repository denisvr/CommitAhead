namespace CommitAhead.Domain.JobAnalyses;

/// <summary>
/// Deliberately has no "Full"/"fully matched" value (invariant 17: a JobGap only ever exists for a
/// requirement that is not fully matched) — a stored JobGap can never represent full matching by
/// construction; there is nothing for the domain to check against, so it doesn't invent an
/// "isFullyMatched" flag anywhere. Not proposing a JobGap for a fully matched requirement in the
/// first place is the AI pipeline's responsibility (Phase 4).
/// </summary>
public enum JobGapMatchLevel
{
    Partial,
    Missing,
    Unknown,
}
