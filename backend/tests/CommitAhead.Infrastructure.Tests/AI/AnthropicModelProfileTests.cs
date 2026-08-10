using CommitAhead.Infrastructure.AI;

namespace CommitAhead.Infrastructure.Tests.AI;

public sealed class AnthropicModelProfileTests
{
    [Fact]
    public void Resolve_ForTheConfiguredHaikuModel_HasATimeoutLongEnoughForFirstCallSchemaCompilation()
    {
        var profile = AnthropicModelProfiles.Resolve("claude-haiku-4-5-20251001");

        Assert.InRange(profile.Timeout, TimeSpan.FromSeconds(180), TimeSpan.FromSeconds(210));
    }
}
