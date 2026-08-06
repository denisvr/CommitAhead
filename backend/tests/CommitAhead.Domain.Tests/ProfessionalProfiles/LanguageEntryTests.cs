using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class LanguageEntryTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var entry = new LanguageEntry(Guid.NewGuid(), "English", LanguageProficiency.Native, null);

        Assert.Equal("English", entry.Language);
        Assert.Equal(LanguageProficiency.Native, entry.Proficiency);
    }

    [Fact]
    public void Constructor_WithBlankLanguage_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new LanguageEntry(Guid.NewGuid(), "   ", LanguageProficiency.B2, null));
    }

    [Fact]
    public void Constructor_WithUndefinedProficiency_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new LanguageEntry(Guid.NewGuid(), "French", (LanguageProficiency)999, null));
    }
}
