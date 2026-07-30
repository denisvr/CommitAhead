namespace CommitAhead.Infrastructure.StudyItems;

/// <summary>
/// Persistence-only row backing IScoringConfigRepository — ScoringConfigOverride is documented
/// as an operational record, not a domain aggregate (docs/domain/model.md), so it has no domain
/// type of its own; the repository translates to/from the domain ScoringWeights value object.
/// </summary>
internal sealed class ScoringConfigOverrideRow
{
    public Guid OwnerUserId { get; set; }
    public int ImportanceWeight { get; set; }
    public int DemandWeight { get; set; }
    public int MasteryGapWeight { get; set; }
}
