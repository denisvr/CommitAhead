using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class EducationEntryTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var entry = new EducationEntry(Guid.NewGuid(), "MIT", "BSc Computer Science", "Computer Science", new YearMonth(2016, 9), new YearMonth(2020, 6), "Cambridge, MA", null);

        Assert.Equal("MIT", entry.Institution);
        Assert.Equal("BSc Computer Science", entry.Degree);
    }

    [Fact]
    public void Constructor_WithBlankInstitution_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new EducationEntry(Guid.NewGuid(), "   ", "Degree", null, null, null, null, null));
    }

    [Fact]
    public void Constructor_WithBlankDegree_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new EducationEntry(Guid.NewGuid(), "MIT", "   ", null, null, null, null, null));
    }

    [Fact]
    public void Constructor_WithoutOptionalFields_AllowsNull()
    {
        var entry = new EducationEntry(Guid.NewGuid(), "MIT", "BSc", null, null, null, null, null);

        Assert.Null(entry.Field);
        Assert.Null(entry.StartDate);
        Assert.Null(entry.EndDate);
    }
}
