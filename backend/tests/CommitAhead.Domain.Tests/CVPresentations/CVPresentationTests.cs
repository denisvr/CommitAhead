using CommitAhead.Domain;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Domain.Tests.CVPresentations;

public class CVPresentationTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = CreatedAt.AddDays(1);

    private static CVPresentation CreatePresentation() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "UK — Senior Backend Engineer",
        "United Kingdom",
        "Senior Backend Engineer",
        "en-GB",
        "modern-one-page",
        summaryOverrideMarkdown: null,
        includePhoto: false,
        includeEmail: true,
        includePhone: true,
        includeAddress: false,
        "dd MMM yyyy",
        pageLimit: 2,
        CreatedAt);

    [Fact]
    public void Constructor_WithValidArguments_CreatesAnEmptyPresentation()
    {
        var presentation = CreatePresentation();

        Assert.Equal("UK — Senior Backend Engineer", presentation.Label);
        Assert.Empty(presentation.ExperienceSelections);
        Assert.Empty(presentation.ProfileLinkSelections);
        Assert.Equal(CreatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt));
    }

    [Fact]
    public void Constructor_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt));
    }

    [Fact]
    public void Constructor_WithEmptyProfessionalProfileId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Label", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositivePageLimit_Throws(int pageLimit)
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", pageLimit, CreatedAt));
    }

    [Theory]
    [InlineData("not-a-real-locale")]
    [InlineData("purple")]
    [InlineData("!!!")]
    public void Constructor_WithAnUnrecognizedLocale_Throws(string locale)
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Label", "Market", null, locale, "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt));
    }

    [Fact]
    public void Constructor_WithBlankLabel_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CVPresentation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt));
    }

    [Fact]
    public void Constructor_WithoutOptionalFields_AllowsNull()
    {
        var presentation = new CVPresentation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, false, false, false, "dd MMM yyyy", 1, CreatedAt);

        Assert.Null(presentation.TargetRole);
        Assert.Null(presentation.SummaryOverrideMarkdown);
    }

    [Fact]
    public void Update_ReplacesEveryFieldAndBumpsUpdatedAt()
    {
        var presentation = CreatePresentation();

        presentation.Update("New label", "Germany", "Backend Engineer", "de-DE", "classic-two-page", "Override", true, false, false, true, "yyyy-MM-dd", 3, UpdatedAt);

        Assert.Equal("New label", presentation.Label);
        Assert.Equal("Germany", presentation.TargetMarket);
        Assert.Equal("de-DE", presentation.Locale);
        Assert.Equal(3, presentation.PageLimit);
        Assert.Equal(UpdatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithABlankLabel_ThrowsAndLeavesEveryFieldUnchanged()
    {
        var presentation = CreatePresentation();

        Assert.Throws<DomainValidationException>(() => presentation.Update(
            "   ", "Germany", "Backend Engineer", "de-DE", "classic-two-page", "Override", true, false, false, true, "yyyy-MM-dd", 3, UpdatedAt));

        Assert.Equal("UK — Senior Backend Engineer", presentation.Label);
        Assert.Equal("United Kingdom", presentation.TargetMarket);
        Assert.Equal(2, presentation.PageLimit);
        Assert.Equal(CreatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithAnUnrecognizedLocale_ThrowsAndLeavesEveryFieldUnchanged()
    {
        var presentation = CreatePresentation();

        Assert.Throws<DomainValidationException>(() => presentation.Update(
            "New label", "Germany", null, "not-a-real-locale", "classic-two-page", null, true, false, false, true, "yyyy-MM-dd", 3, UpdatedAt));

        Assert.Equal("UK — Senior Backend Engineer", presentation.Label);
        Assert.Equal("en-GB", presentation.Locale);
        Assert.Equal(CreatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithANonPositivePageLimit_ThrowsAndLeavesEveryFieldUnchanged()
    {
        var presentation = CreatePresentation();

        Assert.Throws<DomainValidationException>(() => presentation.Update(
            "New label", "Germany", null, "de-DE", "classic-two-page", null, true, false, false, true, "yyyy-MM-dd", 0, UpdatedAt));

        Assert.Equal("UK — Senior Backend Engineer", presentation.Label);
        Assert.Equal(2, presentation.PageLimit);
        Assert.Equal(CreatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceExperienceSelections_PreservesGivenOrder()
    {
        var presentation = CreatePresentation();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        presentation.ReplaceExperienceSelections([second, first], UpdatedAt);

        Assert.Equal([second, first], presentation.ExperienceSelections);
        Assert.Equal(UpdatedAt, presentation.UpdatedAtUtc);
    }

    [Fact]
    public void ReplaceExperienceSelections_WithAnEmptyEntryId_Throws()
    {
        var presentation = CreatePresentation();

        Assert.Throws<DomainValidationException>(() => presentation.ReplaceExperienceSelections([Guid.Empty], UpdatedAt));
    }

    [Fact]
    public void ReplaceExperienceSelections_WithDuplicateEntryIds_Throws()
    {
        var presentation = CreatePresentation();
        var entryId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() => presentation.ReplaceExperienceSelections([entryId, entryId], UpdatedAt));
    }

    [Fact]
    public void ReplaceSkillSelections_WithDuplicateEntryIds_Throws()
    {
        var presentation = CreatePresentation();
        var entryId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() => presentation.ReplaceSkillSelections([entryId, entryId], UpdatedAt));
    }

    [Fact]
    public void ReplaceProfileLinkSelections_WithDuplicateEntryIds_Throws()
    {
        var presentation = CreatePresentation();
        var entryId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() => presentation.ReplaceProfileLinkSelections([entryId, entryId], UpdatedAt));
    }

    [Fact]
    public void ReplaceEducationSelections_WithValidEntries_Succeeds()
    {
        var presentation = CreatePresentation();

        presentation.ReplaceEducationSelections([Guid.NewGuid(), Guid.NewGuid()], UpdatedAt);

        Assert.Equal(2, presentation.EducationSelections.Count);
    }

    [Fact]
    public void ReplaceCertificationSelections_WithValidEntries_Succeeds()
    {
        var presentation = CreatePresentation();

        presentation.ReplaceCertificationSelections([Guid.NewGuid()], UpdatedAt);

        Assert.Single(presentation.CertificationSelections);
    }

    [Fact]
    public void ReplaceProjectSelections_WithValidEntries_Succeeds()
    {
        var presentation = CreatePresentation();

        presentation.ReplaceProjectSelections([Guid.NewGuid()], UpdatedAt);

        Assert.Single(presentation.ProjectSelections);
    }

    [Fact]
    public void ReplaceLanguageSelections_WithValidEntries_Succeeds()
    {
        var presentation = CreatePresentation();

        presentation.ReplaceLanguageSelections([Guid.NewGuid()], UpdatedAt);

        Assert.Single(presentation.LanguageSelections);
    }
}
