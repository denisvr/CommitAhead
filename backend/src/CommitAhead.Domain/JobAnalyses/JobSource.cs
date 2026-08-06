using CommitAhead.Domain;

namespace CommitAhead.Domain.JobAnalyses;

/// <summary>
/// Discriminated union matching JobAnalysis's raw input (CONTEXT.md "JobSource") — PastedText or
/// UploadedFile. Plain C# types with no serialization concerns, mirroring
/// <c>CommitAhead.Domain.StudyItems.StudyItemDetails</c>: an empty abstract base with each variant
/// as its own sealed subtype, no shared "kind" discriminator field on the base itself. JSON
/// (de)serialization is an Infrastructure-only responsibility.
/// </summary>
public abstract class JobSource
{
}

/// <summary>The job posting text pasted directly by the user.</summary>
public sealed class PastedText : JobSource
{
    public string Content { get; }

    public PastedText(string content)
    {
        Content = TextValidation.RequireNonBlank(content, nameof(content), ValidationLimits.JobSourceTextMaxLength);
    }
}

/// <summary>
/// A PDF uploaded to Storage. Text is extracted once at upload time (ADR-0010) — this type never
/// re-parses; <see cref="ExtractedText"/> is the only thing the AI provider (Phase 4) ever reads.
/// <see cref="StorageObjectKey"/> is a backend-generated quarantine key, never the original
/// filename (docs/security/threat-model.md).
/// </summary>
public sealed class UploadedFile : JobSource
{
    private const string RequiredMimeType = "application/pdf";

    public string StorageObjectKey { get; }
    public string OriginalFileName { get; }
    public string MimeType { get; }
    public string ExtractedText { get; }

    public UploadedFile(string storageObjectKey, string originalFileName, string mimeType, string extractedText)
    {
        StorageObjectKey = TextValidation.RequireNonBlank(storageObjectKey, nameof(storageObjectKey), ValidationLimits.ShortTextMaxLength);
        OriginalFileName = TextValidation.RequireNonBlank(originalFileName, nameof(originalFileName), ValidationLimits.ShortTextMaxLength);
        MimeType = ValidateMimeType(mimeType);
        ExtractedText = TextValidation.RequireNonBlank(extractedText, nameof(extractedText), ValidationLimits.JobSourceTextMaxLength);
    }

    /// <summary>The only MIME type Phase 3 ever accepts — normalized (trimmed, lowercased) before comparing, not merely required to be non-blank.</summary>
    private static string ValidateMimeType(string mimeType)
    {
        var normalized = (mimeType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != RequiredMimeType)
        {
            throw new DomainValidationException($"mimeType must be '{RequiredMimeType}'.");
        }

        return normalized;
    }
}
