using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Domain.Tests.JobAnalyses;

public class JobRequirementTests
{
    private static JobRequirement CreateRequirement(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "5+ years of C# experience", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C# experience.");

    [Fact]
    public void Constructor_WithValidArguments_StoresEveryField()
    {
        var id = Guid.NewGuid();
        var requirement = CreateRequirement(id);

        Assert.Equal(id, requirement.Id);
        Assert.Equal("5+ years of C# experience", requirement.Text);
        Assert.Equal(JobRequirementKind.Technical, requirement.Kind);
        Assert.Equal(JobRequirementPriority.Required, requirement.Priority);
        Assert.Equal("Must have 5+ years of C# experience.", requirement.SourceExcerpt);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.Empty, "Text", JobRequirementKind.Technical, JobRequirementPriority.Required, "Excerpt"));
    }

    [Fact]
    public void Constructor_WithAnUndefinedKind_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.NewGuid(), "Text", (JobRequirementKind)999, JobRequirementPriority.Required, "Excerpt"));
    }

    [Fact]
    public void Constructor_WithAnUndefinedPriority_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.NewGuid(), "Text", JobRequirementKind.Technical, (JobRequirementPriority)999, "Excerpt"));
    }

    [Fact]
    public void Constructor_WithBlankText_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.NewGuid(), "   ", JobRequirementKind.Technical, JobRequirementPriority.Required, "Excerpt"));
    }

    [Fact]
    public void Constructor_WithBlankSourceExcerpt_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.NewGuid(), "Text", JobRequirementKind.Technical, JobRequirementPriority.Required, "   "));
    }

    [Fact]
    public void Constructor_WithSourceExcerptOverTheLimit_Throws()
    {
        var tooLong = new string('a', ValidationLimits.SourceExcerptMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new JobRequirement(
            Guid.NewGuid(), "Text", JobRequirementKind.Technical, JobRequirementPriority.Required, tooLong));
    }
}
