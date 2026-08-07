using System.Text.Json;
using System.Text.Json.Serialization;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Infrastructure.JobAnalyses;

/// <summary>
/// Owns the "kind" discriminator and the mapping to/from each concrete JobSource subtype — mirrors
/// StudyItemDetailsJsonConverter exactly. The Domain types carry no serialization attributes and
/// never reference System.Text.Json; this converter, and the DTOs below, are the entire boundary.
/// </summary>
internal sealed class JobSourceJsonConverter : JsonConverter<JobSource>
{
    private static readonly JsonSerializerOptions DtoOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public override JobSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();

        return kind switch
        {
            "PastedText" => root.Deserialize<PastedTextDto>(DtoOptions)!.ToDomain(),
            "UploadedFile" => root.Deserialize<UploadedFileDto>(DtoOptions)!.ToDomain(),
            _ => throw new JsonException($"Unknown JobSource kind '{kind}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, JobSource value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case PastedText source:
                JsonSerializer.Serialize(writer, PastedTextDto.FromDomain(source), DtoOptions);
                return;
            case UploadedFile source:
                JsonSerializer.Serialize(writer, UploadedFileDto.FromDomain(source), DtoOptions);
                return;
            default:
                throw new JsonException($"Unknown JobSource type '{value.GetType().Name}'.");
        }
    }

    private sealed record PastedTextDto(string Kind, string Content)
    {
        public static PastedTextDto FromDomain(PastedText source) => new("PastedText", source.Content);

        public PastedText ToDomain() => new(Content);
    }

    private sealed record UploadedFileDto(string Kind, string StorageObjectKey, string OriginalFileName, string MimeType, string ExtractedText)
    {
        public static UploadedFileDto FromDomain(UploadedFile source) => new(
            "UploadedFile", source.StorageObjectKey, source.OriginalFileName, source.MimeType, source.ExtractedText);

        public UploadedFile ToDomain() => new(StorageObjectKey, OriginalFileName, MimeType, ExtractedText);
    }
}
