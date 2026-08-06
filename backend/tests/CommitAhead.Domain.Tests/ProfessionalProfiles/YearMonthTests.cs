using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class YearMonthTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_WithMonthOutOfRange_Throws(int month)
    {
        Assert.Throws<DomainValidationException>(() => new YearMonth(2024, month));
    }

    [Fact]
    public void Equality_SameYearAndMonth_AreEqual()
    {
        Assert.Equal(new YearMonth(2024, 6), new YearMonth(2024, 6));
    }

    [Fact]
    public void Equality_DifferentMonth_AreNotEqual()
    {
        Assert.NotEqual(new YearMonth(2024, 6), new YearMonth(2024, 7));
    }

    [Fact]
    public void ComparisonOperators_OrderByYearThenMonth()
    {
        var earlier = new YearMonth(2023, 12);
        var later = new YearMonth(2024, 1);

        Assert.True(earlier < later);
        Assert.True(later > earlier);
        Assert.True(earlier <= new YearMonth(2023, 12));
        Assert.True(later >= new YearMonth(2024, 1));
    }
}
