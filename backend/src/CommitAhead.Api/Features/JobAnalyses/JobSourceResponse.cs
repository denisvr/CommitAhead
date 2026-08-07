using System.Text.Json.Serialization;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Api.Features.JobAnalyses;

/// <summary>
/// The read-only wire-contract counterpart of JobSource (ADR-0002's discriminated union). Kept
/// separate from Infrastructure's own DTOs for the same union — Api must not depend on
/// Infrastructure — but mirrors its "kind" discriminator, matching StudyItemDetailsDto's pattern.
/// Response-only: there is deliberately no ToDomain()/write-side counterpart for UploadedFile in
/// this slice — see CreateJobAnalysisRequest for why.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PastedTextResponse), "PastedText")]
[JsonDerivedType(typeof(UploadedFileResponse), "UploadedFile")]
public abstract record JobSourceResponse
{
    public static JobSourceResponse FromDomain(JobSource source) => source switch
    {
        PastedText s => new PastedTextResponse(s.Content),
        UploadedFile s => new UploadedFileResponse(s.OriginalFileName, s.MimeType, s.ExtractedText),
        _ => throw new ArgumentOutOfRangeException(nameof(source), $"Unknown JobSource type '{source.GetType().Name}'."),
    };
}

public sealed record PastedTextResponse(string Content) : JobSourceResponse;

/// <summary>Omits StorageObjectKey — an internal Storage quarantine key, never client-facing (docs/security/threat-model.md).</summary>
public sealed record UploadedFileResponse(string OriginalFileName, string MimeType, string ExtractedText) : JobSourceResponse;
