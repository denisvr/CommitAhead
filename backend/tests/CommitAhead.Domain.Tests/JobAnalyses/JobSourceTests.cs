using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Domain.Tests.JobAnalyses;

public class PastedTextTests
{
    [Fact]
    public void Constructor_WithValidContent_TrimsAndStores()
    {
        var source = new PastedText("  Senior Backend Engineer at Acme.  ");

        Assert.Equal("Senior Backend Engineer at Acme.", source.Content);
    }

    [Fact]
    public void Constructor_WithBlankContent_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new PastedText("   "));
    }

    [Fact]
    public void Constructor_WithContentOverTheLimit_Throws()
    {
        var tooLong = new string('a', ValidationLimits.JobSourceTextMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new PastedText(tooLong));
    }
}

public class UploadedFileTests
{
    private static UploadedFile CreateUploadedFile(string mimeType = "application/pdf") =>
        new("quarantine/abc123", "job-posting.pdf", mimeType, "Extracted job posting text.");

    [Fact]
    public void Constructor_WithValidArguments_StoresEveryField()
    {
        var source = CreateUploadedFile();

        Assert.Equal("quarantine/abc123", source.StorageObjectKey);
        Assert.Equal("job-posting.pdf", source.OriginalFileName);
        Assert.Equal("application/pdf", source.MimeType);
        Assert.Equal("Extracted job posting text.", source.ExtractedText);
    }

    [Theory]
    [InlineData("APPLICATION/PDF")]
    [InlineData(" application/pdf ")]
    public void Constructor_NormalizesMimeTypeCasingAndWhitespace(string mimeType)
    {
        var source = CreateUploadedFile(mimeType);

        Assert.Equal("application/pdf", source.MimeType);
    }

    [Theory]
    [InlineData("application/msword")]
    [InlineData("text/plain")]
    [InlineData("")]
    public void Constructor_WithAMimeTypeOtherThanApplicationPdf_Throws(string mimeType)
    {
        Assert.Throws<DomainValidationException>(() => CreateUploadedFile(mimeType));
    }

    [Fact]
    public void Constructor_WithBlankStorageObjectKey_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new UploadedFile("   ", "job-posting.pdf", "application/pdf", "Extracted text."));
    }

    [Fact]
    public void Constructor_WithBlankExtractedText_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new UploadedFile("quarantine/abc123", "job-posting.pdf", "application/pdf", "   "));
    }

    [Fact]
    public void Constructor_WithExtractedTextOverTheLimit_Throws()
    {
        var tooLong = new string('a', ValidationLimits.JobSourceTextMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new UploadedFile("quarantine/abc123", "job-posting.pdf", "application/pdf", tooLong));
    }
}
