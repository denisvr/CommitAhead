using System.Text.Json;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.StudyItems;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

/// <summary>
/// StudyItemProposal.AcceptedDetails' converter — reuses StudyItems' own StudyItemDetailsJsonConverter
/// (internal, same assembly) rather than a second copy of its kind-discriminator mapping; only the
/// null-handling wrapper around it is new, since StudyItem.Details itself is never nullable and has
/// no existing nullable-JSONB converter to share.
/// </summary>
internal sealed class NullableStudyItemDetailsValueConverter : ValueConverter<StudyItemDetails?, string?>
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new StudyItemDetailsJsonConverter() } };

    public NullableStudyItemDetailsValueConverter()
        : base(
            details => details == null ? null : JsonSerializer.Serialize(details, Options),
            json => json == null ? null : JsonSerializer.Deserialize<StudyItemDetails>(json, Options))
    {
    }
}
