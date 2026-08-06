using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class SkillTests
{
    [Theory]
    [InlineData("C#", "c")]
    [InlineData("  Node.js  ", "node-js")]
    [InlineData("PostgreSQL", "postgresql")]
    public void Constructor_NormalizesDisplayNameIntoKey(string displayName, string expectedKey)
    {
        var skill = new Skill(Guid.NewGuid(), displayName, SkillCategory.Tool, null);

        Assert.Equal(expectedKey, skill.NormalizedKey);
    }

    [Fact]
    public void Constructor_WithBlankDisplayName_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new Skill(Guid.NewGuid(), "   ", SkillCategory.Tool, null));
    }

    [Fact]
    public void Constructor_WithUndefinedCategory_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new Skill(Guid.NewGuid(), "Go", (SkillCategory)999, null));
    }

    [Fact]
    public void Constructor_WithUndefinedProficiency_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new Skill(Guid.NewGuid(), "Go", SkillCategory.Language, (SkillProficiency)999));
    }

    [Fact]
    public void Constructor_WithoutProficiency_AllowsNull()
    {
        var skill = new Skill(Guid.NewGuid(), "Go", SkillCategory.Language, null);

        Assert.Null(skill.Proficiency);
    }
}
