using System.Text;
using CommitAhead.Application.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Creates a JobAnalysis from an uploaded PDF: validates, uploads to Storage under a
/// backend-generated quarantine key, extracts text once, and persists — all in one request
/// (ADR-0010's "text extraction happens once during the upload request"). Every rejection after a
/// successful Storage upload triggers a best-effort delete of that same known key before the
/// rejection is reported, so no orphan is created for a request this use case itself rejects
/// (ADR-0011's "failed uploads have their Storage objects deleted immediately").
/// </summary>
public sealed class CreateJobAnalysisFromUploadUseCase
{
    private const int MaxUploadedFileSizeBytes = 5 * 1024 * 1024;
    private const string CanonicalMimeType = "application/pdf";
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly IJobAnalysisRepository _repository;
    private readonly IJobPostingStorage _storage;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateJobAnalysisFromUploadUseCase> _logger;

    public CreateJobAnalysisFromUploadUseCase(
        IJobAnalysisRepository repository,
        IJobPostingStorage storage,
        IPdfTextExtractor pdfTextExtractor,
        ICurrentUser currentUser,
        ILogger<CreateJobAnalysisFromUploadUseCase> logger)
    {
        _repository = repository;
        _storage = storage;
        _pdfTextExtractor = pdfTextExtractor;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Guid> ExecuteAsync(
        string title, Stream fileContent, string originalFileName, string declaredMimeType, string? notesMarkdown, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(originalFileName) || !originalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("originalFileName must end in .pdf.");
        }

        if (declaredMimeType is null || declaredMimeType.Trim().ToLowerInvariant() != CanonicalMimeType)
        {
            throw new DomainValidationException($"declaredMimeType must be '{CanonicalMimeType}'.");
        }

        using var buffer = await CopyBoundedAsync(fileContent, cancellationToken);
        await RequirePdfMagicBytesAsync(buffer, cancellationToken);

        var storageObjectKey = $"{_currentUser.UserId:D}/{Guid.NewGuid():N}";
        var uploadAttempted = false;

        try
        {
            uploadAttempted = true;
            await _storage.UploadAsync(storageObjectKey, buffer, CanonicalMimeType, cancellationToken);

            buffer.Position = 0;
            var extractedText = await _pdfTextExtractor.ExtractTextAsync(buffer, cancellationToken);

            var jobSource = new UploadedFile(storageObjectKey, originalFileName, CanonicalMimeType, extractedText);
            var analysis = new JobAnalysis(Guid.NewGuid(), _currentUser.UserId, title, jobSource, notesMarkdown, DateTime.UtcNow);
            await _repository.AddAsync(analysis, cancellationToken);

            return analysis.Id;
        }
        catch (Exception ex)
        {
            if (uploadAttempted)
            {
                await CleanUpAsync(storageObjectKey);
            }

            // A genuine cancellation of THIS request must propagate unchanged, never be
            // reinterpreted as a validation failure (matches LogoutUseCase's own
            // OperationCanceledException-vs-cancellationToken distinction).
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            if (ex is PdfExtractionException pdfException)
            {
                throw new DomainValidationException(DescribeRejection(pdfException.Reason));
            }

            // Anything else — an existing DomainValidationException from UploadedFile's own
            // constructor, a Storage HttpRequestException, a DbUpdateException, an unrelated
            // cancellation — propagates unchanged.
            throw;
        }
    }

    private async Task CleanUpAsync(string storageObjectKey)
    {
        try
        {
            // Independent of the caller's own cancellationToken — if the caller cancelled, that
            // token is already signalled and cleanup must still get a chance to run.
            using var cleanupCts = new CancellationTokenSource(CleanupTimeout);
            await _storage.DeleteAsync(storageObjectKey, cleanupCts.Token);
        }
        catch (Exception cleanupException)
        {
            _logger.LogWarning(
                "Best-effort Storage cleanup failed for a rejected upload. StorageObjectKey: {StorageObjectKey}. Exception type: {ExceptionType}",
                storageObjectKey, cleanupException.GetType().Name);
        }
    }

    private static async Task<MemoryStream> CopyBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[8192];
        long totalRead = 0;

        while (true)
        {
            var remainingAllowed = MaxUploadedFileSizeBytes + 1 - totalRead;
            if (remainingAllowed <= 0)
            {
                break;
            }

            var bytesRead = await source.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, remainingAllowed)), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;
        }

        if (totalRead == 0)
        {
            throw new DomainValidationException("The uploaded file must not be empty.");
        }

        if (totalRead > MaxUploadedFileSizeBytes)
        {
            throw new DomainValidationException($"The uploaded file must be at most {MaxUploadedFileSizeBytes} bytes.");
        }

        buffer.Position = 0;
        return buffer;
    }

    private static async Task RequirePdfMagicBytesAsync(MemoryStream buffer, CancellationToken cancellationToken)
    {
        var magic = new byte[5];
        var magicBytesRead = await buffer.ReadAsync(magic.AsMemory(0, magic.Length), cancellationToken);
        buffer.Position = 0;

        if (magicBytesRead < magic.Length || Encoding.ASCII.GetString(magic) != "%PDF-")
        {
            throw new DomainValidationException("The uploaded file is not a valid PDF.");
        }
    }

    private static string DescribeRejection(PdfExtractionFailureReason reason) => reason switch
    {
        PdfExtractionFailureReason.Malformed => "The uploaded file could not be parsed as a valid PDF.",
        PdfExtractionFailureReason.Encrypted => "The uploaded PDF is password-protected and cannot be processed.",
        PdfExtractionFailureReason.ImageOnly => "The uploaded PDF contains no extractable text.",
        PdfExtractionFailureReason.TooManyPages => "The uploaded PDF has too many pages.",
        PdfExtractionFailureReason.TimedOut => "The uploaded PDF took too long to process.",
        PdfExtractionFailureReason.TooMuchText => "The uploaded PDF's extracted text exceeds the allowed length.",
        _ => "The uploaded PDF could not be processed.",
    };
}
