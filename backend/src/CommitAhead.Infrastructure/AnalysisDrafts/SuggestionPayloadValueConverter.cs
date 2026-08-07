using System.Text.Json;
using CommitAhead.Domain.AnalysisDrafts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

/// <summary>SuggestionProposal.ProposedPayload's converter — always present, unlike AcceptedPayload.</summary>
internal sealed class SuggestionPayloadValueConverter : ValueConverter<SuggestionPayload, string>
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new SuggestionPayloadJsonConverter() } };

    public SuggestionPayloadValueConverter()
        : base(
            payload => JsonSerializer.Serialize(payload, Options),
            json => JsonSerializer.Deserialize<SuggestionPayload>(json, Options)!)
    {
    }
}

/// <summary>
/// SuggestionProposal.AcceptedPayload's converter — null until Accept, unlike ProposedPayload
/// (always present), so EF's nullability-checked HasConversion overload needs a distinct
/// nullable-typed converter rather than reusing SuggestionPayloadValueConverter above.
/// </summary>
internal sealed class NullableSuggestionPayloadValueConverter : ValueConverter<SuggestionPayload?, string?>
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new SuggestionPayloadJsonConverter() } };

    public NullableSuggestionPayloadValueConverter()
        : base(
            payload => payload == null ? null : JsonSerializer.Serialize(payload, Options),
            json => json == null ? null : JsonSerializer.Deserialize<SuggestionPayload>(json, Options))
    {
    }
}
