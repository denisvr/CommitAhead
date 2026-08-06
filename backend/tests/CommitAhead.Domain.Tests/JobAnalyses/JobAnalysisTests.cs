using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Domain.Tests.JobAnalyses;

public class JobAnalysisTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = CreatedAt.AddDays(1);

    private static JobAnalysis CreateAnalysis(string title = "Senior Backend Engineer @ Acme") =>
        new(Guid.NewGuid(), Guid.NewGuid(), title, new PastedText("Job posting text."), null, CreatedAt);

    private static JobRequirement CreateRequirement(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "5+ years of C#", JobRequirementKind.Technical, JobRequirementPriority.Required, "Must have 5+ years of C#.");

    private static JobGap CreateGap(Guid requirementId, Guid? id = null) => new(
        id ?? Guid.NewGuid(), requirementId, JobGapMatchLevel.Partial, JobGapSeverity.High, "Only 2 years of C#.");

    [Fact]
    public void Constructor_WithValidArguments_CreatesAnAnalysisWithNoRequirementsOrGaps()
    {
        var analysis = CreateAnalysis();

        Assert.Empty(analysis.Requirements);
        Assert.Empty(analysis.Gaps);
        Assert.Equal(CreatedAt, analysis.CreatedAtUtc);
        Assert.Equal(CreatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobAnalysis(Guid.Empty, Guid.NewGuid(), "Title", new PastedText("Text"), null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobAnalysis(Guid.NewGuid(), Guid.Empty, "Title", new PastedText("Text"), null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithBlankTitle_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobAnalysis(Guid.NewGuid(), Guid.NewGuid(), "   ", new PastedText("Text"), null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithNullJobSource_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new JobAnalysis(Guid.NewGuid(), Guid.NewGuid(), "Title", null!, null, CreatedAt));
    }

    [Fact]
    public void Update_ReplacesTitleAndNotesAndBumpsUpdatedAt()
    {
        var analysis = CreateAnalysis();

        analysis.Update("New title", "Some notes.", UpdatedAt);

        Assert.Equal("New title", analysis.Title);
        Assert.Equal("Some notes.", analysis.NotesMarkdown);
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithABlankTitle_ThrowsAndLeavesEveryFieldUnchanged()
    {
        var analysis = CreateAnalysis("Original title");

        Assert.Throws<DomainValidationException>(() => analysis.Update("   ", "Notes", UpdatedAt));

        Assert.Equal("Original title", analysis.Title);
        Assert.Null(analysis.NotesMarkdown);
        Assert.Equal(CreatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void AddRequirement_AddsToRequirementsAndBumpsUpdatedAt()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();

        analysis.AddRequirement(requirement, UpdatedAt);

        Assert.Same(requirement, Assert.Single(analysis.Requirements));
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void AddRequirement_WithNull_ThrowsAndLeavesRequirementsUnchanged()
    {
        var analysis = CreateAnalysis();

        Assert.Throws<DomainValidationException>(() => analysis.AddRequirement(null!, UpdatedAt));

        Assert.Empty(analysis.Requirements);
        Assert.Equal(CreatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void AddRequirement_WithADuplicateId_ThrowsAndLeavesRequirementsUnchanged()
    {
        var analysis = CreateAnalysis();
        var id = Guid.NewGuid();
        analysis.AddRequirement(CreateRequirement(id), UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.AddRequirement(CreateRequirement(id), UpdatedAt));

        Assert.Single(analysis.Requirements);
    }

    [Fact]
    public void RemoveRequirement_AlsoRemovesItsGaps()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        analysis.AddGap(CreateGap(requirement.Id), UpdatedAt);
        var removalTimestamp = UpdatedAt.AddDays(1);

        analysis.RemoveRequirement(requirement.Id, removalTimestamp);

        Assert.Empty(analysis.Requirements);
        Assert.Empty(analysis.Gaps);
        Assert.Equal(removalTimestamp, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveRequirement_WithAnEmptyId_ThrowsAndLeavesStateUnchanged()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.RemoveRequirement(Guid.Empty, UpdatedAt.AddDays(1)));

        Assert.Equal([requirement], analysis.Requirements);
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveRequirement_WithANonexistentId_ThrowsAndLeavesStateUnchanged()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.RemoveRequirement(Guid.NewGuid(), UpdatedAt.AddDays(1)));

        Assert.Equal([requirement], analysis.Requirements);
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveRequirement_LeavesUnrelatedRequirementsAndGapsIntact()
    {
        var analysis = CreateAnalysis();
        var keptRequirement = CreateRequirement();
        var removedRequirement = CreateRequirement();
        analysis.AddRequirement(keptRequirement, UpdatedAt);
        analysis.AddRequirement(removedRequirement, UpdatedAt);
        var keptGap = CreateGap(keptRequirement.Id);
        analysis.AddGap(keptGap, UpdatedAt);

        analysis.RemoveRequirement(removedRequirement.Id, UpdatedAt);

        Assert.Equal([keptRequirement], analysis.Requirements);
        Assert.Equal([keptGap], analysis.Gaps);
    }

    [Fact]
    public void AddGap_ReferencingARequirementNotOnThisAnalysis_ThrowsAndLeavesGapsUnchanged()
    {
        var analysis = CreateAnalysis();

        Assert.Throws<DomainValidationException>(() => analysis.AddGap(CreateGap(Guid.NewGuid()), UpdatedAt));

        Assert.Empty(analysis.Gaps);
        Assert.Equal(CreatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void AddGap_WithNull_Throws()
    {
        var analysis = CreateAnalysis();

        Assert.Throws<DomainValidationException>(() => analysis.AddGap(null!, UpdatedAt));
    }

    [Fact]
    public void AddGap_WithADuplicateId_ThrowsAndLeavesGapsUnchanged()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        var gapId = Guid.NewGuid();
        analysis.AddGap(CreateGap(requirement.Id, gapId), UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.AddGap(CreateGap(requirement.Id, gapId), UpdatedAt));

        Assert.Single(analysis.Gaps);
    }

    [Fact]
    public void AddGap_ForARequirementOnThisAnalysis_Succeeds()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        var gap = CreateGap(requirement.Id);

        analysis.AddGap(gap, UpdatedAt);

        Assert.Same(gap, Assert.Single(analysis.Gaps));
    }

    [Fact]
    public void RemoveGap_WithAnEmptyId_ThrowsAndLeavesStateUnchanged()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        var gap = CreateGap(requirement.Id);
        analysis.AddGap(gap, UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.RemoveGap(Guid.Empty, UpdatedAt.AddDays(1)));

        Assert.Equal([gap], analysis.Gaps);
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveGap_WithANonexistentId_ThrowsAndLeavesStateUnchanged()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        var gap = CreateGap(requirement.Id);
        analysis.AddGap(gap, UpdatedAt);

        Assert.Throws<DomainValidationException>(() => analysis.RemoveGap(Guid.NewGuid(), UpdatedAt.AddDays(1)));

        Assert.Equal([gap], analysis.Gaps);
        Assert.Equal(UpdatedAt, analysis.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveGap_RemovesOnlyTheTargetedGap()
    {
        var analysis = CreateAnalysis();
        var requirement = CreateRequirement();
        analysis.AddRequirement(requirement, UpdatedAt);
        var keptGap = CreateGap(requirement.Id);
        var removedGap = CreateGap(requirement.Id);
        analysis.AddGap(keptGap, UpdatedAt);
        analysis.AddGap(removedGap, UpdatedAt);

        analysis.RemoveGap(removedGap.Id, UpdatedAt);

        Assert.Equal([keptGap], analysis.Gaps);
    }
}
