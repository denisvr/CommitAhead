using System.Text.Json;
using CommitAhead.Domain.StudyItems;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CommitAhead.Infrastructure.StudyItems;

internal sealed class StudyItemDetailsValueConverter : ValueConverter<StudyItemDetails, string>
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new StudyItemDetailsJsonConverter() } };

    public StudyItemDetailsValueConverter()
        : base(
            details => JsonSerializer.Serialize(details, Options),
            json => JsonSerializer.Deserialize<StudyItemDetails>(json, Options)!)
    {
    }
}
