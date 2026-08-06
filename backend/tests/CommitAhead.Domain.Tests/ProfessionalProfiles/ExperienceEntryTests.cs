using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class ExperienceEntryTests
{
    private static ExperienceEntry CreateEntry(IReadOnlyList<Guid>? skillIds = null) => new(
        Guid.NewGuid(),
        "Acme Corp",
        client: null,
        "Senior Engineer",
        EmploymentType.Permanent,
        new YearMonth(2020, 1),
        endDate: null,
        location: "Remote",
        WorkMode.Remote,
        "Led backend platform work.",
        ["Shipped the v2 API"],
        skillIds ?? []);

    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var entry = CreateEntry();

        Assert.Equal("Acme Corp", entry.Company);
        Assert.Null(entry.EndDate);
    }

    [Fact]
    public void Constructor_WithBlankCompany_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ExperienceEntry(
            Guid.NewGuid(), "   ", null, "Role", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], []));
    }

    [Fact]
    public void Constructor_WithUndefinedEmploymentType_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ExperienceEntry(
            Guid.NewGuid(), "Acme", null, "Role", (EmploymentType)999, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], []));
    }

    [Fact]
    public void Constructor_WithUndefinedWorkMode_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ExperienceEntry(
            Guid.NewGuid(), "Acme", null, "Role", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, (WorkMode)999, "Summary", [], []));
    }

    [Fact]
    public void Constructor_WithEmptySkillId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateEntry([Guid.Empty]));
    }

    [Fact]
    public void Constructor_WithDuplicateSkillIds_Throws()
    {
        var skillId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() => CreateEntry([skillId, skillId]));
    }
}
