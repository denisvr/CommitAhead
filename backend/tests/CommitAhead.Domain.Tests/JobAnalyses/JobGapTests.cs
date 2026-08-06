using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Domain.Tests.JobAnalyses;

public class JobGapTests
{
    private static JobGap CreateGap(Guid? id = null, Guid? requirementId = null) => new(
        id ?? Guid.NewGuid(), requirementId ?? Guid.NewGuid(), JobGapMatchLevel.Partial, JobGapSeverity.High, "Only 2 years of C# experience.");

    [Fact]
    public void Constructor_WithValidArguments_StoresEveryField()
    {
        var id = Guid.NewGuid();
        var requirementId = Guid.NewGuid();
        var gap = CreateGap(id, requirementId);

        Assert.Equal(id, gap.Id);
        Assert.Equal(requirementId, gap.RequirementId);
        Assert.Equal(JobGapMatchLevel.Partial, gap.MatchLevel);
        Assert.Equal(JobGapSeverity.High, gap.Severity);
        Assert.Equal("Only 2 years of C# experience.", gap.Rationale);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.Empty, Guid.NewGuid(), JobGapMatchLevel.Partial, JobGapSeverity.High, "Rationale"));
    }

    [Fact]
    public void Constructor_WithEmptyRequirementId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.NewGuid(), Guid.Empty, JobGapMatchLevel.Partial, JobGapSeverity.High, "Rationale"));
    }

    [Fact]
    public void Constructor_WithAnUndefinedMatchLevel_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.NewGuid(), Guid.NewGuid(), (JobGapMatchLevel)999, JobGapSeverity.High, "Rationale"));
    }

    [Fact]
    public void Constructor_WithAnUndefinedSeverity_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.NewGuid(), Guid.NewGuid(), JobGapMatchLevel.Partial, (JobGapSeverity)999, "Rationale"));
    }

    [Fact]
    public void Constructor_WithBlankRationale_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.NewGuid(), Guid.NewGuid(), JobGapMatchLevel.Partial, JobGapSeverity.High, "   "));
    }

    [Fact]
    public void Constructor_WithRationaleOverTheLimit_Throws()
    {
        var tooLong = new string('a', ValidationLimits.GapRationaleMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new JobGap(
            Guid.NewGuid(), Guid.NewGuid(), JobGapMatchLevel.Partial, JobGapSeverity.High, tooLong));
    }
}
