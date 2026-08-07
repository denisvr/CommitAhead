using CommitAhead.Application.Identity;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

internal sealed class FakeCurrentUserAccessToken : ICurrentUserAccessToken
{
    public required string Value { get; init; }
}
