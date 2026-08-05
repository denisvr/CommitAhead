using CommitAhead.Domain;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.EvidenceLinks;

public class EvidenceLinkTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static EvidenceLink CreateLink(decimal weight = 2m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.JobAnalysis, Guid.NewGuid(), Guid.NewGuid(), weight, "Mentioned in job posting", Now);

    [Fact]
    public void Constructor_WithValidArguments_CreatesTheLink()
    {
        var link = CreateLink(3.5m);

        Assert.Equal(3.5m, link.Weight);
        Assert.Equal("Mentioned in job posting", link.Rationale);
        Assert.Equal(EvidenceSourceType.JobAnalysis, link.SourceType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5.1)]
    public void Constructor_WithWeightOutOfRange_Throws(double weight)
    {
        Assert.Throws<DomainValidationException>(() => CreateLink((decimal)weight));
    }

    [Fact]
    public void Constructor_WithBlankRationale_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new EvidenceLink(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.InterviewNote, Guid.NewGuid(), Guid.NewGuid(), 1m, "   ", Now));
    }

    [Fact]
    public void Constructor_WithEmptyTargetStudyItemId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new EvidenceLink(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.CVPresentation, Guid.NewGuid(), Guid.Empty, 1m, "reason", Now));
    }

    [Fact]
    public void Constructor_WithRationaleLongerThanMaxLength_Throws()
    {
        var rationale = new string('a', ValidationLimits.EvidenceLinkRationaleMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new EvidenceLink(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.InterviewNote, Guid.NewGuid(), Guid.NewGuid(), 1m, rationale, Now));
    }

    [Fact]
    public void Constructor_WithRationaleAtMaxLength_Succeeds()
    {
        var rationale = new string('a', ValidationLimits.EvidenceLinkRationaleMaxLength);

        var link = new EvidenceLink(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.InterviewNote, Guid.NewGuid(), Guid.NewGuid(), 1m, rationale, Now);

        Assert.Equal(rationale, link.Rationale);
    }
}
