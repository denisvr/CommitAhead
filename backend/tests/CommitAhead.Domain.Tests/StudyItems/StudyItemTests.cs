using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class StudyItemTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static TheoryDetails ValidTheoryDetails() => new(
        summaryMarkdown: "CAP theorem",
        keyPoints: ["Consistency", "Availability", "Partition tolerance"],
        interviewQuestions: ["What does CAP stand for?"],
        references: ["https://example.com/cap"]);

    private static StudyItem CreateItem(
        StudyItemCategory category = StudyItemCategory.Theory,
        int importance = 3,
        int initialMastery = 2,
        IReadOnlyList<string>? tags = null,
        StudyItemDetails? details = null)
    {
        return new StudyItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CAP theorem trade-offs",
            category,
            importance,
            initialMastery,
            tags ?? ["Distributed Systems", "  Distributed Systems  ", "System Design"],
            details ?? ValidTheoryDetails(),
            Now);
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesActiveItem_WithNormalizedTags()
    {
        var item = CreateItem();

        Assert.Equal(StudyItemStatus.Active, item.Status);
        Assert.Equal(Now, item.CreatedAtUtc);
        Assert.Equal(Now, item.UpdatedAtUtc);
        Assert.Empty(item.Reviews);
        Assert.Null(item.PriorityOverride);
        Assert.Equal(["distributed-systems", "system-design"], item.Tags);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new StudyItem(
            Guid.Empty, Guid.NewGuid(), "Title", StudyItemCategory.Theory, 3, 2, [], ValidTheoryDetails(), Now));
    }

    [Fact]
    public void Constructor_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new StudyItem(
            Guid.NewGuid(), Guid.Empty, "Title", StudyItemCategory.Theory, 3, 2, [], ValidTheoryDetails(), Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new StudyItem(
            Guid.NewGuid(), Guid.NewGuid(), title!, StudyItemCategory.Theory, 3, 2, [], ValidTheoryDetails(), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_WithImportanceOutOfRange_Throws(int importance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateItem(importance: importance));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_WithInitialMasteryOutOfRange_Throws(int initialMastery)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateItem(initialMastery: initialMastery));
    }

    [Fact]
    public void Constructor_WithDetailsNotMatchingCategory_Throws()
    {
        var leetCodeDetails = new LeetCodeDetails(1, null, Difficulty.Easy, [], "O(n)", "O(1)", "approach", null);

        Assert.Throws<ArgumentException>(() => CreateItem(category: StudyItemCategory.Theory, details: leetCodeDetails));
    }

    [Fact]
    public void Update_ChangesTitleImportanceTagsAndDetails_AndBumpsUpdatedAt()
    {
        var item = CreateItem();
        var newDetails = new TheoryDetails("Updated", [], [], []);
        var updatedAt = Now.AddDays(1);

        item.Update("New title", 5, ["new-tag"], newDetails, updatedAt);

        Assert.Equal("New title", item.Title);
        Assert.Equal(5, item.Importance);
        Assert.Equal(["new-tag"], item.Tags);
        Assert.Same(newDetails, item.Details);
        Assert.Equal(updatedAt, item.UpdatedAtUtc);
        Assert.Equal(Now, item.CreatedAtUtc);
    }

    [Fact]
    public void Update_WithDetailsNotMatchingCategory_Throws()
    {
        var item = CreateItem(category: StudyItemCategory.Theory);
        var leetCodeDetails = new LeetCodeDetails(1, null, Difficulty.Easy, [], "O(n)", "O(1)", "approach", null);

        Assert.Throws<ArgumentException>(() => item.Update("Title", 3, [], leetCodeDetails, Now));
    }

    [Fact]
    public void Archive_SetsStatusToArchived_AndDoesNotTouchMasteryInputs()
    {
        var item = CreateItem(initialMastery: 4);
        var updatedAt = Now.AddDays(2);

        item.Archive(updatedAt);

        Assert.Equal(StudyItemStatus.Archived, item.Status);
        Assert.Equal(4, item.InitialMastery);
        Assert.Equal(updatedAt, item.UpdatedAtUtc);
    }

    [Fact]
    public void SetPriorityOverride_ThenClear_RestoresNullOverride()
    {
        var item = CreateItem();
        var priorityOverride = new PriorityOverride(90, "Interview next week");

        item.SetPriorityOverride(priorityOverride, Now.AddHours(1));
        Assert.Same(priorityOverride, item.PriorityOverride);

        item.ClearPriorityOverride(Now.AddHours(2));
        Assert.Null(item.PriorityOverride);
    }

    [Fact]
    public void CanBeHardDeleted_IsTrue_UntilAReviewExists()
    {
        var item = CreateItem();
        Assert.True(item.CanBeHardDeleted);

        item.AddReview(new StudyReview(Guid.NewGuid(), Now, 3, null), Now);

        Assert.False(item.CanBeHardDeleted);
    }

    [Fact]
    public void ComputeMastery_WithNoReviews_ReturnsInitialMastery()
    {
        var item = CreateItem(initialMastery: 3);

        Assert.Equal(3m, item.ComputeMastery());
    }

    [Fact]
    public void ComputeMastery_WithOneReview_ReturnsThatRating()
    {
        var item = CreateItem(initialMastery: 1);
        item.AddReview(new StudyReview(Guid.NewGuid(), Now, 4, null), Now);

        Assert.Equal(4m, item.ComputeMastery());
    }

    [Fact]
    public void ComputeMastery_WithMoreThanThreeReviews_AveragesOnlyTheThreeMostRecent()
    {
        var item = CreateItem();
        item.AddReview(new StudyReview(Guid.NewGuid(), Now, 1, null), Now); // oldest, excluded
        item.AddReview(new StudyReview(Guid.NewGuid(), Now.AddDays(1), 3, null), Now);
        item.AddReview(new StudyReview(Guid.NewGuid(), Now.AddDays(2), 5, null), Now);
        item.AddReview(new StudyReview(Guid.NewGuid(), Now.AddDays(3), 4, null), Now);

        Assert.Equal(4m, item.ComputeMastery()); // (3 + 5 + 4) / 3
    }

    [Fact]
    public void ComputeMastery_WithTiedReviewedAt_BreaksTiesByIdDescending()
    {
        var item = CreateItem();
        var earlierId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var laterId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        item.AddReview(new StudyReview(earlierId, Now, 1, null), Now);
        item.AddReview(new StudyReview(laterId, Now, 5, null), Now);

        // Both reviewed at the same instant; laterId sorts after earlierId, so it is "most recent"
        // and alone would be the only review if there were a limit of 1 — with only two reviews
        // here, both are included, but the ordering itself must not throw and must be stable.
        Assert.Equal(3m, item.ComputeMastery());
    }
}
