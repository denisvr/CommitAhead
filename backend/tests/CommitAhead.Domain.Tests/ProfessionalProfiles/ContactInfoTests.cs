using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class ContactInfoTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var contactInfo = new ContactInfo("Ada Lovelace", "ada@example.com", "+44 20 7946 0958", "London, UK", "photos/ada.jpg");

        Assert.Equal("Ada Lovelace", contactInfo.Name);
        Assert.Equal("ada@example.com", contactInfo.Email);
        Assert.Equal("+44 20 7946 0958", contactInfo.Phone);
        Assert.Equal("London, UK", contactInfo.Address);
        Assert.Equal("photos/ada.jpg", contactInfo.PhotoStorageKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string value)
    {
        Assert.Throws<DomainValidationException>(() => new ContactInfo(value, "ada@example.com", null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankEmail_Throws(string value)
    {
        Assert.Throws<DomainValidationException>(() => new ContactInfo("Ada Lovelace", value, null, null, null));
    }

    [Fact]
    public void Constructor_WithoutOptionalFields_AllowsNull()
    {
        var contactInfo = new ContactInfo("Ada Lovelace", "ada@example.com", null, null, null);

        Assert.Null(contactInfo.Phone);
        Assert.Null(contactInfo.Address);
        Assert.Null(contactInfo.PhotoStorageKey);
    }

    [Fact]
    public void Constructor_WithBlankOptionalPhone_TreatsAsNull()
    {
        var contactInfo = new ContactInfo("Ada Lovelace", "ada@example.com", "   ", null, null);

        Assert.Null(contactInfo.Phone);
    }
}
