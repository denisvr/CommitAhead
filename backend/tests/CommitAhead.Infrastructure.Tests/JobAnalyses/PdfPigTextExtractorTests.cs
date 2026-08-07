using CommitAhead.Application.JobAnalyses;
using CommitAhead.Infrastructure.JobAnalyses;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

/// <summary>
/// Exercises the real PdfPig 0.1.15 integration against hand-crafted minimal PDF fixtures
/// (MinimalPdfFixtures) — never fakes, since the point is proving the real library's behavior for
/// each rejection reason.
///
/// Encrypted is covered by MinimalPdfFixtures.Encrypted() — a small, real, password-protected PDF
/// committed as a binary fixture (JobAnalyses/Fixtures/encrypted.pdf; see its own README.md for
/// exactly how it was generated), not hand-rolled crypto. Opened without its password, PdfPig
/// fails authentication and throws PdfDocumentEncryptedException for real — a real fixture-driven
/// test against the real library, not just the source's catch clause.
///
/// Not covered here, deliberately:
/// - TimedOut: there is no deterministic way to force PdfPig's synchronous parser to run past the
///   10-second budget without a pathological fixture; CreateJobAnalysisFromUploadUseCaseTests
///   covers the use case's own handling of a TimedOut failure via a fake extractor instead, which
///   says nothing about PdfPig's real timing behavior.
/// </summary>
public class PdfPigTextExtractorTests
{
    private readonly PdfPigTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractTextAsync_WithAValidSinglePagePdf_ReturnsItsText()
    {
        var pdf = MinimalPdfFixtures.SinglePage("Hello World");

        var text = await _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None);

        Assert.Equal("Hello World 0", text);
    }

    [Fact]
    public async Task ExtractTextAsync_WithAPasswordProtectedPdf_ThrowsEncrypted()
    {
        var pdf = MinimalPdfFixtures.Encrypted();

        var exception = await Assert.ThrowsAsync<PdfExtractionException>(
            () => _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None));

        Assert.Equal(PdfExtractionFailureReason.Encrypted, exception.Reason);
    }

    [Fact]
    public async Task ExtractTextAsync_WithMalformedBytes_ThrowsMalformed()
    {
        var exception = await Assert.ThrowsAsync<PdfExtractionException>(
            () => _extractor.ExtractTextAsync(new MemoryStream(MinimalPdfFixtures.Malformed), CancellationToken.None));

        Assert.Equal(PdfExtractionFailureReason.Malformed, exception.Reason);
    }

    [Fact]
    public async Task ExtractTextAsync_WithAPdfThatHasNoContents_ThrowsImageOnly()
    {
        var pdf = MinimalPdfFixtures.MultiPage(1, null);

        var exception = await Assert.ThrowsAsync<PdfExtractionException>(
            () => _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None));

        Assert.Equal(PdfExtractionFailureReason.ImageOnly, exception.Reason);
    }

    /// <summary>
    /// PdfPig's own page.Text carries no trailing separator, so joining pages with a bare append
    /// would read the last word of one page and the first word of the next as a single merged
    /// word — proving the extractor's own newline join (not PdfPig itself) keeps them apart.
    /// </summary>
    [Fact]
    public async Task ExtractTextAsync_WithMultiplePages_DoesNotMergeWordsAcrossThePageBoundary()
    {
        var pdf = MinimalPdfFixtures.MultiPageWithDistinctText("endsInWordA", "startsWithWordB");

        var text = await _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None);

        Assert.DoesNotContain("endsInWordAstartsWithWordB", text);
        Assert.Contains("endsInWordA", text);
        Assert.Contains("startsWithWordB", text);
    }

    [Fact]
    public async Task ExtractTextAsync_WithMoreThanTwentyPages_ThrowsTooManyPages()
    {
        var pdf = MinimalPdfFixtures.MultiPage(21, "Page");

        var exception = await Assert.ThrowsAsync<PdfExtractionException>(
            () => _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None));

        Assert.Equal(PdfExtractionFailureReason.TooManyPages, exception.Reason);
    }

    /// <summary>Deterministic, against the real extractor: three pages of ~20,000 characters each, so the total genuinely exceeds the 50,000-character cap — proving explicit rejection, not truncation.</summary>
    [Fact]
    public async Task ExtractTextAsync_WhenExtractedTextExceedsTheCharacterLimit_ThrowsTooMuchText()
    {
        var repeatedWords = string.Join(" ", Enumerable.Repeat("word", 4000));
        var pdf = MinimalPdfFixtures.MultiPage(3, repeatedWords);

        var exception = await Assert.ThrowsAsync<PdfExtractionException>(
            () => _extractor.ExtractTextAsync(new MemoryStream(pdf), CancellationToken.None));

        Assert.Equal(PdfExtractionFailureReason.TooMuchText, exception.Reason);
    }
}
