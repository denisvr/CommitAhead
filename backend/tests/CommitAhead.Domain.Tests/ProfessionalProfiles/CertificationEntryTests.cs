using CommitAhead.Domain;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Domain.Tests.ProfessionalProfiles;

public class CertificationEntryTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var entry = new CertificationEntry(Guid.NewGuid(), "AWS Certified Developer", "Amazon", new YearMonth(2022, 3), new YearMonth(2025, 3), "ABC123", "https://aws.amazon.com/verify");

        Assert.Equal("AWS Certified Developer", entry.Name);
        Assert.Equal("https://aws.amazon.com/verify", entry.Url);
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CertificationEntry(Guid.NewGuid(), "   ", "Amazon", null, null, null, null));
    }

    [Fact]
    public void Constructor_WithBlankIssuingOrganisation_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CertificationEntry(Guid.NewGuid(), "Cert", "   ", null, null, null, null));
    }

    [Fact]
    public void Constructor_WithNonHttpUrl_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new CertificationEntry(Guid.NewGuid(), "Cert", "Amazon", null, null, null, "ftp://example.com"));
    }

    [Fact]
    public void Constructor_WithoutOptionalFields_AllowsNull()
    {
        var entry = new CertificationEntry(Guid.NewGuid(), "Cert", "Amazon", null, null, null, null);

        Assert.Null(entry.Url);
        Assert.Null(entry.CredentialId);
    }
}
