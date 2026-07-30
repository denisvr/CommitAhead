using CommitAhead.Application.Identity;

namespace CommitAhead.Application.Tests.Identity;

public sealed class StubCurrentUser : ICurrentUser
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
