namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// The three weighted terms EffectiveScorePolicy sums, exposed individually for detail-view
/// rendering (CLAUDE.md: EffectiveScore/Demand/Mastery are backend-computed and never
/// recomputed in React — the breakdown itself must be computed here, not reassembled from raw
/// weights client-side).
/// </summary>
public sealed record ScoreBreakdown(decimal ImportanceContribution, decimal DemandContribution, decimal MasteryGapContribution, int Total);
