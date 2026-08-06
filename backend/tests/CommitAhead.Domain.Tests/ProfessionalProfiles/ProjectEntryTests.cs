using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class ProjectEntryTests
{
    private static ProjectEntry CreateEntry(IEnumerable<Guid>? skillIds = null) => new(
        Guid.NewGuid(), "CommitAhead", "Author", new YearMonth(2026, 1), null, "An interview-prep app.", "https://github.com/example/commitahead", skillIds ?? []);

    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var entry = CreateEntry();

        Assert.Equal("CommitAhead", entry.Name);
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProjectEntry(Guid.NewGuid(), "   ", null, null, null, "Description", null, []));
    }

    [Fact]
    public void Constructor_WithBlankDescriptionMarkdown_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProjectEntry(Guid.NewGuid(), "Name", null, null, null, "   ", null, []));
    }

    [Fact]
    public void Constructor_WithDuplicateSkillIds_Throws()
    {
        var skillId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() => CreateEntry([skillId, skillId]));
    }
}
