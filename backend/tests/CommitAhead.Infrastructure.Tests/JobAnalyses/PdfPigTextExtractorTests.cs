using CommitAhead.Application.JobAnalyses;
using CommitAhead.Infrastructure.JobAnalyses;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

/// <summary>
/// Exercises the real PdfPig 0.1.15 integration against hand-crafted minimal PDF fixtures
/// (MinimalPdfFixtures) — never fakes, since the point is proving the real library's behavior for
/// each rejection reason.
///
/// Not covered here, deliberately:
/// - Encrypted: a genuinely encrypted PDF requires real PDF-spec encryption (RC4/AES with a
///   correctly computed owner/user key) that is impractical to hand-craft as a byte literal; the
///   Encrypted mapping itself (PdfDocumentEncryptedException -> PdfExtractionFailureReason.Encrypted)
///   is exercised indirectly by the exact catch clause in PdfPigTextExtractor's source, but no
///   fixture-driven test exists for it in this slice.
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
