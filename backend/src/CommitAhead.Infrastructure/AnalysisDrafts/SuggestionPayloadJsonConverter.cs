using System.Text.Json;
using System.Text.Json.Serialization;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

/// <summary>
/// Owns the "kind" discriminator and the mapping to/from each concrete SuggestionPayload subtype
/// — mirrors JobSourceJsonConverter/StudyItemDetailsJsonConverter exactly. The Domain types carry
/// no serialization attributes and never reference System.Text.Json; this converter, and the DTOs
/// below, are the entire boundary.
/// </summary>
internal sealed class SuggestionPayloadJsonConverter : JsonConverter<SuggestionPayload>
{
    private static readonly JsonSerializerOptions DtoOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public override SuggestionPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();

        return kind switch
        {
            "StructuredSuggestion" => root.Deserialize<StructuredSuggestionDto>(DtoOptions)!.ToDomain(),
            "AdvisorySuggestion" => root.Deserialize<AdvisorySuggestionDto>(DtoOptions)!.ToDomain(),
            _ => throw new JsonException($"Unknown SuggestionPayload kind '{kind}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, SuggestionPayload value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case StructuredSuggestion payload:
                JsonSerializer.Serialize(writer, StructuredSuggestionDto.FromDomain(payload), DtoOptions);
                return;
            case AdvisorySuggestion payload:
                JsonSerializer.Serialize(writer, AdvisorySuggestionDto.FromDomain(payload), DtoOptions);
                return;
            default:
                throw new JsonException($"Unknown SuggestionPayload type '{value.GetType().Name}'.");
        }
    }

    private sealed record StructuredSuggestionDto(string Kind, StructuredSuggestionCommandType CommandType, string PayloadJson)
    {
        public static StructuredSuggestionDto FromDomain(StructuredSuggestion payload) => new("StructuredSuggestion", payload.CommandType, payload.PayloadJson);

        public StructuredSuggestion ToDomain() => new(CommandType, PayloadJson);
    }

    private sealed record AdvisorySuggestionDto(string Kind, string Markdown)
    {
        public static AdvisorySuggestionDto FromDomain(AdvisorySuggestion payload) => new("AdvisorySuggestion", payload.Markdown);

        public AdvisorySuggestion ToDomain() => new(Markdown);
    }
}
