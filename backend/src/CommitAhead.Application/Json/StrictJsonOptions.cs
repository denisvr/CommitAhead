using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommitAhead.Application.Json;

/// <summary>
/// The strict <see cref="JsonSerializerOptions"/> shared by every parser in this assembly that
/// interprets untrusted-ish JSON into Domain objects — both AI-proposed output (Application/AI/)
/// and user-finalised Apply decisions (Application/AnalysisDrafts/). Unknown properties are
/// rejected outright (never silently ignored), and enums must be the exact member name — no
/// integer ordinals accepted. Deliberately separate from Infrastructure's own JSONB persistence
/// converters (StudyItemDetailsJsonConverter, SuggestionPayloadJsonConverter) — this is boundary
/// validation, the same role an upload/DTO use case already plays, not a second persistence-
/// serialization path. Application may reference System.Text.Json directly: it is a BCL type, not
/// EF Core/Npgsql/ASP.NET Core/Supabase.
/// </summary>
public static class StrictJsonOptions
{
    public static readonly JsonSerializerOptions Strict = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };
}
