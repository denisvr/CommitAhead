using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Domain.Tests.EvidenceLinks;

public class DemandPolicyTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static EvidenceLink LinkWithWeight(decimal weight) =>
        new(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.JobAnalysis, Guid.NewGuid(), Guid.NewGuid(), weight, "reason", Now);

    [Fact]
    public void Compute_WithNoLinks_ReturnsZero()
    {
        Assert.Equal(0m, DemandPolicy.Compute([]));
    }

    [Fact]
    public void Compute_SumsWeights()
    {
        var links = new[] { LinkWithWeight(1m), LinkWithWeight(2m) };

        Assert.Equal(3m, DemandPolicy.Compute(links));
    }

    [Fact]
    public void Compute_ClampsAtFive()
    {
        var links = new[] { LinkWithWeight(4m), LinkWithWeight(4m) };

        Assert.Equal(5m, DemandPolicy.Compute(links));
    }
}
