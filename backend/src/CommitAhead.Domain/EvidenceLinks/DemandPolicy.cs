namespace CommitAhead.Domain.EvidenceLinks;

/// <summary>Pure formula from docs/domain/model.md: min(Σ EvidenceLink.weight targeting the item, 5).</summary>
public static class DemandPolicy
{
    public static decimal Compute(IEnumerable<EvidenceLink> links)
    {
        return Math.Min(links.Sum(link => link.Weight), 5m);
    }
}
