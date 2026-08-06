using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class ProfileLinkTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, "Personal", "https://github.com/example");

        Assert.Equal(ProfileLinkKind.GitHub, link.Kind);
        Assert.Equal("https://github.com/example", link.Url);
    }

    [Fact]
    public void Constructor_WithUndefinedKind_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProfileLink(Guid.NewGuid(), (ProfileLinkKind)999, null, "https://github.com/example"));
    }

    [Fact]
    public void Constructor_WithNonHttpUrl_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new ProfileLink(Guid.NewGuid(), ProfileLinkKind.GitHub, null, "ftp://example.com"));
    }

    [Fact]
    public void Constructor_WithoutLabel_AllowsNull()
    {
        var link = new ProfileLink(Guid.NewGuid(), ProfileLinkKind.Blog, null, "https://example.com/blog");

        Assert.Null(link.Label);
    }
}
