using System.Text.Json;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CommitAhead.Infrastructure.JobAnalyses;

internal sealed class JobSourceValueConverter : ValueConverter<JobSource, string>
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JobSourceJsonConverter() } };

    public JobSourceValueConverter()
        : base(
            source => JsonSerializer.Serialize(source, Options),
            json => JsonSerializer.Deserialize<JobSource>(json, Options)!)
    {
    }
}
