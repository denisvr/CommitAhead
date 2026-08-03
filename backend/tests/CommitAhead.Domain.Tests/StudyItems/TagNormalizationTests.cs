using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

/// <summary>
/// TagNormalizer is internal, so these drive it through StudyItem.Tags — the same public
/// surface StudyItemTests.Constructor_WithValidArguments_CreatesActiveItem_WithNormalizedTags
/// already exercises once, but with one case per rule from the documented policy (trim,
/// lowercase, kebab-case, deduplicate — see TagNormalizer's allowed-character-policy comment).
/// </summary>
public class TagNormalizationTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IReadOnlyList<string> NormalizeViaStudyItem(IReadOnlyList<string> tags) => new StudyItem(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Title",
        StudyItemCategory.Theory,
        importance: 3,
        initialMastery: 2,
        tags,
        new TheoryDetails("Summary", [], [], []),
        Now).Tags;

    [Fact]
    public void SurroundingWhitespace_IsTrimmed()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["  Distributed Systems  "]));
    }

    [Fact]
    public void RepeatedInternalWhitespace_CollapsesToOneHyphen()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["Distributed    Systems"]));
    }

    [Fact]
    public void Underscores_BecomeHyphens()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["distributed_systems"]));
    }

    [Fact]
    public void Punctuation_BecomesHyphens()
    {
        Assert.Equal(["c-basics"], NormalizeViaStudyItem(["C++ Basics"]));
    }

    [Fact]
    public void RepeatedSeparators_OfMixedKinds_CollapseToOneHyphen()
    {
        Assert.Equal(["c-basics"], NormalizeViaStudyItem(["c++__ basics"]));
    }

    [Fact]
    public void AlreadyNormalizedValue_IsUnchanged()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["distributed-systems"]));
    }

    [Fact]
    public void DuplicatesAfterNormalization_AreDeduplicated()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["Distributed Systems", "distributed_systems", "  distributed-systems  "]));
    }

    [Fact]
    public void ValueThatIsEntirelySeparators_NormalizesToEmptyAndIsDropped()
    {
        Assert.Empty(NormalizeViaStudyItem(["   ", "___", "+++"]));
    }

    [Fact]
    public void LeadingAndTrailingSeparators_AreTrimmedNotHyphenated()
    {
        Assert.Equal(["distributed-systems"], NormalizeViaStudyItem(["_distributed_systems_"]));
    }
}
