using System.Text;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Auth;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class CreateJobAnalysisFromUploadUseCaseTests
{
    private const int MaxUploadedFileSizeBytes = 5 * 1024 * 1024;

    private static MemoryStream ValidPdfContent(int totalBytes = 20) => new(Encoding.ASCII.GetBytes("%PDF-" + new string('A', totalBytes - 5)));

    private static CreateJobAnalysisFromUploadUseCase CreateUseCase(
        FakeJobAnalysisRepository repository,
        FakeJobPostingStorage storage,
        FakePdfTextExtractor extractor,
        Guid ownerUserId,
        ILogger<CreateJobAnalysisFromUploadUseCase>? logger = null)
        => new(
            repository,
            storage,
            extractor,
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
            logger ?? NullLogger<CreateJobAnalysisFromUploadUseCase>.Instance);

    [Fact]
    public async Task ExecuteAsync_WithAValidPdf_CreatesAnAnalysisAndReturnsItsId()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor { TextToReturn = "Extracted job posting text." };
        var ownerUserId = Guid.NewGuid();
        var useCase = CreateUseCase(repository, storage, extractor, ownerUserId);

        var id = await useCase.ExecuteAsync("Senior Backend Engineer", ValidPdfContent(), "posting.pdf", "application/pdf", "Some notes.", CancellationToken.None);

        var created = Assert.Single(repository.Analyses);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
        var jobSource = Assert.IsType<UploadedFile>(created.JobSource);
        Assert.Equal("posting.pdf", jobSource.OriginalFileName);
        Assert.Equal("application/pdf", jobSource.MimeType);
        Assert.Equal("Extracted job posting text.", jobSource.ExtractedText);
        var uploadCall = Assert.Single(storage.UploadCalls);
        Assert.Equal(jobSource.StorageObjectKey, uploadCall.Key);
        Assert.StartsWith($"{ownerUserId:D}/", uploadCall.Key, StringComparison.Ordinal);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnEmptyFile_ThrowsBeforeAnyStorageCall()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", new MemoryStream(), "posting.pdf", "application/pdf", null, CancellationToken.None));

        Assert.Empty(storage.UploadCalls);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WithANonPdfFilename_ThrowsBeforeAnyStorageCall()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.txt", "application/pdf", null, CancellationToken.None));

        Assert.Empty(storage.UploadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAWrongDeclaredMimeType_ThrowsBeforeAnyStorageCall()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.pdf", "text/plain", null, CancellationToken.None));

        Assert.Empty(storage.UploadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithContentOverTheSizeLimit_IsRejectedByTheBoundedCopy_NotByATrustedLength()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());
        var oversizedContent = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-" + new string('A', MaxUploadedFileSizeBytes)));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", oversizedContent, "posting.pdf", "application/pdf", null, CancellationToken.None));

        Assert.Empty(storage.UploadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutThePdfMagicBytes_ThrowsBeforeAnyStorageCall()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());
        var notAPdf = new MemoryStream(Encoding.ASCII.GetBytes("Not a PDF at all, just plain text."));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", notAPdf, "posting.pdf", "application/pdf", null, CancellationToken.None));

        Assert.Empty(storage.UploadCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExtractionFails_DeletesTheKnownUploadedKey_AndThrowsASafeValidationException()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor { ExceptionToThrow = new PdfExtractionException(PdfExtractionFailureReason.Malformed) };
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.pdf", "application/pdf", null, CancellationToken.None));

        // A safe, fixed message per rejection reason — never the raw PdfExtractionException text.
        Assert.Equal("The uploaded file could not be parsed as a valid PDF.", exception.Message);
        var uploadedKey = Assert.Single(storage.UploadCalls).Key;
        Assert.Equal([uploadedKey], storage.DeletedKeys);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBothExtractionAndTheCleanupDeleteFail_LogsTheStorageObjectKey_NeverTheException()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage { ExceptionToThrowOnDelete = new HttpRequestException("Storage is unreachable") };
        var extractor = new FakePdfTextExtractor { ExceptionToThrow = new PdfExtractionException(PdfExtractionFailureReason.Malformed) };
        var logger = new RecordingLogger<CreateJobAnalysisFromUploadUseCase>();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid(), logger);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.pdf", "application/pdf", null, CancellationToken.None));

        var uploadedKey = Assert.Single(storage.UploadCalls).Key;
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains(uploadedKey, entry.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The ambiguous case: UploadAsync itself throws, so it's unknown from the caller's side
    /// whether Storage actually persisted the bytes before failing. Cleanup still runs against the
    /// exact known key regardless — deleting a key that was never successfully stored is harmless
    /// (FakeJobPostingStorage records the call before throwing, exactly like a real HTTP client
    /// would have sent the bytes before an error response/exception arrived). The original
    /// exception is a genuine infrastructure failure, not a validation problem, so it propagates
    /// unchanged rather than being wrapped into a DomainValidationException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenUploadItselfFails_StillAttemptsCleanupWithTheKnownKey_AndPropagatesTheOriginalException()
    {
        var repository = new FakeJobAnalysisRepository();
        var uploadException = new HttpRequestException("Storage is unreachable");
        var storage = new FakeJobPostingStorage { ExceptionToThrowOnUpload = uploadException };
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.pdf", "application/pdf", null, CancellationToken.None));

        Assert.Same(uploadException, thrown);
        var uploadedKey = Assert.Single(storage.UploadCalls).Key;
        Assert.Equal([uploadedKey], storage.DeletedKeys);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheCallerCancels_StillRunsCleanup_ButPropagatesTheCancellationUnchanged()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        using var cts = new CancellationTokenSource();
        var extractor = new FakePdfTextExtractor
        {
            ExceptionToThrow = new OperationCanceledException("Caller cancelled mid-extraction.", cts.Token),
            BeforeThrow = () => cts.Cancel(),
        };
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync("Title", ValidPdfContent(), "posting.pdf", "application/pdf", null, cts.Token));

        var uploadedKey = Assert.Single(storage.UploadCalls).Key;
        Assert.Equal([uploadedKey], storage.DeletedKeys);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_BothStorageAndTheExtractor_ReceiveTheExactSameCompleteBytes()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var extractor = new FakePdfTextExtractor();
        var useCase = CreateUseCase(repository, storage, extractor, Guid.NewGuid());
        var originalBytes = Encoding.ASCII.GetBytes("%PDF-" + new string('B', 500));

        await useCase.ExecuteAsync("Title", new MemoryStream(originalBytes), "posting.pdf", "application/pdf", null, CancellationToken.None);

        Assert.Equal(originalBytes, Assert.Single(storage.UploadCalls).Bytes);
        Assert.Equal(originalBytes, extractor.ReceivedBytes);
    }
}
