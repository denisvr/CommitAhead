using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class ProfessionalProfileTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = CreatedAt.AddDays(1);

    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static ProfessionalProfile CreateProfile() =>
        new(Guid.NewGuid(), Guid.NewGuid(), ValidContactInfo(), "Backend engineer.", CreatedAt);

    private static Skill CreateSkill(string displayName) => new(Guid.NewGuid(), displayName, SkillCategory.Language, null);

    private static ExperienceEntry CreateExperience(IEnumerable<Guid>? skillIds = null) => new(
        Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], skillIds ?? []);

    private static ProjectEntry CreateProject(IEnumerable<Guid>? skillIds = null) => new(
        Guid.NewGuid(), "Side project", null, null, null, "Description", null, skillIds ?? []);

    [Fact]
    public void Constructor_WithValidArguments_CreatesAnEmptyProfile()
    {
        var profile = CreateProfile();

        Assert.Empty(profile.Experience);
        Assert.Empty(profile.Skills);
        Assert.Equal(CreatedAt, profile.CreatedAtUtc);
        Assert.Equal(CreatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProfessionalProfile(Guid.Empty, Guid.NewGuid(), ValidContactInfo(), "Summary", CreatedAt));
    }

    [Fact]
    public void Constructor_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProfessionalProfile(Guid.NewGuid(), Guid.Empty, ValidContactInfo(), "Summary", CreatedAt));
    }

    [Fact]
    public void Constructor_WithBlankSummary_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProfessionalProfile(Guid.NewGuid(), Guid.NewGuid(), ValidContactInfo(), "   ", CreatedAt));
    }

    [Fact]
    public void UpdateContactInfo_ReplacesContactInfoAndBumpsUpdatedAt()
    {
        var profile = CreateProfile();
        var newContactInfo = new ContactInfo("Grace Hopper", "grace@example.com", null, null, null);

        profile.UpdateContactInfo(newContactInfo, UpdatedAt);

        Assert.Same(newContactInfo, profile.ContactInfo);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateSummary_WithBlankValue_ThrowsAndLeavesSummaryUnchanged()
    {
        var profile = CreateProfile();

        Assert.Throws<DomainValidationException>(() => profile.UpdateSummary("   ", UpdatedAt));
        Assert.Equal("Backend engineer.", profile.SummaryMarkdown);
        Assert.Equal(CreatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceEducation_WithANullEntry_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<DomainValidationException>(() => profile.ReplaceEducation([null!], UpdatedAt));
    }

    [Fact]
    public void ReplaceEducation_WithDuplicateIds_Throws()
    {
        var profile = CreateProfile();
        var id = Guid.NewGuid();
        var first = new EducationEntry(id, "MIT", "BSc", null, null, null, null, null);
        var second = new EducationEntry(id, "Stanford", "MSc", null, null, null, null, null);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceEducation([first, second], UpdatedAt));
    }

    [Fact]
    public void ReplaceEducation_WithValidEntries_ReplacesTheCollectionAndBumpsUpdatedAt()
    {
        var profile = CreateProfile();
        var entry = new EducationEntry(Guid.NewGuid(), "MIT", "BSc", null, null, null, null, null);

        profile.ReplaceEducation([entry], UpdatedAt);

        Assert.Single(profile.Education);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceLanguages_WithDuplicateIds_Throws()
    {
        var profile = CreateProfile();
        var id = Guid.NewGuid();
        var first = new LanguageEntry(id, "English", LanguageProficiency.Native, null);
        var second = new LanguageEntry(id, "French", LanguageProficiency.B2, null);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceLanguages([first, second], UpdatedAt));
    }

    [Fact]
    public void ReplaceCertifications_WithDuplicateIds_Throws()
    {
        var profile = CreateProfile();
        var id = Guid.NewGuid();
        var first = new CertificationEntry(id, "Cert A", "Org", null, null, null, null);
        var second = new CertificationEntry(id, "Cert B", "Org", null, null, null, null);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceCertifications([first, second], UpdatedAt));
    }

    [Fact]
    public void ReplaceProfileLinks_WithDuplicateIds_Throws()
    {
        var profile = CreateProfile();
        var id = Guid.NewGuid();
        var first = new ProfileLink(id, ProfileLinkKind.GitHub, null, "https://github.com/a");
        var second = new ProfileLink(id, ProfileLinkKind.Blog, null, "https://example.com/b");

        Assert.Throws<DomainValidationException>(() => profile.ReplaceProfileLinks([first, second], UpdatedAt));
    }

    [Fact]
    public void ReplaceExperience_ReferencingANonexistentSkill_Throws()
    {
        var profile = CreateProfile();
        var experience = CreateExperience([Guid.NewGuid()]);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceExperience([experience], UpdatedAt));
    }

    [Fact]
    public void ReplaceExperience_ReferencingAnExistingSkill_Succeeds()
    {
        var profile = CreateProfile();
        var skill = CreateSkill("C#");
        profile.ReplaceSkills([skill], UpdatedAt);
        var experience = CreateExperience([skill.Id]);

        profile.ReplaceExperience([experience], UpdatedAt);

        Assert.Single(profile.Experience);
    }

    [Fact]
    public void ReplaceExperience_WithAFailingReference_LeavesTheAggregateUnchanged()
    {
        var profile = CreateProfile();
        var skill = CreateSkill("C#");
        profile.ReplaceSkills([skill], UpdatedAt);
        var validExperience = CreateExperience([skill.Id]);
        profile.ReplaceExperience([validExperience], UpdatedAt);
        var laterUpdatedAt = UpdatedAt.AddDays(1);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceExperience([CreateExperience([Guid.NewGuid()])], laterUpdatedAt));

        Assert.Single(profile.Experience);
        Assert.Same(validExperience, profile.Experience[0]);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceProjects_ReferencingANonexistentSkill_Throws()
    {
        var profile = CreateProfile();
        var project = CreateProject([Guid.NewGuid()]);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceProjects([project], UpdatedAt));
    }

    [Fact]
    public void ReplaceSkills_WithDuplicateNormalizedKeys_ThrowsAndLeavesSkillsUnchanged()
    {
        var profile = CreateProfile();
        var original = CreateSkill("C#");
        profile.ReplaceSkills([original], UpdatedAt);
        var laterUpdatedAt = UpdatedAt.AddDays(1);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceSkills([CreateSkill("C#"), CreateSkill("C#")], laterUpdatedAt));

        Assert.Single(profile.Skills);
        Assert.Same(original, profile.Skills[0]);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceSkills_RemovingASkillStillReferencedByExperience_ThrowsAndLeavesSkillsUnchanged()
    {
        var profile = CreateProfile();
        var skill = CreateSkill("C#");
        profile.ReplaceSkills([skill], UpdatedAt);
        profile.ReplaceExperience([CreateExperience([skill.Id])], UpdatedAt);
        var laterUpdatedAt = UpdatedAt.AddDays(1);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceSkills([], laterUpdatedAt));

        Assert.Single(profile.Skills);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceSkills_RemovingASkillStillReferencedByAProject_ThrowsAndLeavesSkillsUnchanged()
    {
        var profile = CreateProfile();
        var skill = CreateSkill("C#");
        profile.ReplaceSkills([skill], UpdatedAt);
        profile.ReplaceProjects([CreateProject([skill.Id])], UpdatedAt);
        var laterUpdatedAt = UpdatedAt.AddDays(1);

        Assert.Throws<DomainValidationException>(() => profile.ReplaceSkills([], laterUpdatedAt));

        Assert.Single(profile.Skills);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceSkills_RemovingAnUnreferencedSkill_Succeeds()
    {
        var profile = CreateProfile();
        var skill = CreateSkill("C#");
        profile.ReplaceSkills([skill], UpdatedAt);

        profile.ReplaceSkills([], UpdatedAt.AddDays(1));

        Assert.Empty(profile.Skills);
    }
}
