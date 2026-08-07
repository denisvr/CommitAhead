using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommitAhead.Application.AI;

/// <summary>
/// The strict <see cref="JsonSerializerOptions"/> every untrusted-AI-output parser in this
/// namespace shares: unknown properties are rejected outright (never silently ignored), and enums
/// must be the exact member name — no integer ordinals accepted. Deliberately separate from
/// Infrastructure's own JSONB persistence converters (StudyItemDetailsJsonConverter,
/// SuggestionPayloadJsonConverter) — this interprets untrusted external AI output into Domain
/// objects, the same boundary-validation role an upload/DTO use case already plays, not a second
/// persistence-serialization path. Application may reference System.Text.Json directly: it is a
/// BCL type, not EF Core/Npgsql/ASP.NET Core/Supabase.
/// </summary>
internal static class AiJsonOptions
{
    public static readonly JsonSerializerOptions Strict = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };
}
