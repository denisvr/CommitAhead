using CommitAhead.Infrastructure.AI;

namespace CommitAhead.Infrastructure.Tests.AI;

public sealed class AnthropicBaseAddressTests
{
    [Fact]
    public void Resolve_OutsideE2E_WithNoConfiguredValue_ReturnsProductionDefault()
    {
        var resolved = AnthropicBaseAddress.Resolve(configuredValue: null, environmentName: "Production");

        Assert.Equal(new Uri(AnthropicBaseAddress.ProductionDefault), resolved);
    }

    [Fact]
    public void Resolve_OutsideE2E_WithHttpsValue_ReturnsThatValue()
    {
        var resolved = AnthropicBaseAddress.Resolve("https://api.anthropic.com/", "Docker");

        Assert.Equal(new Uri("https://api.anthropic.com/"), resolved);
    }

    [Theory]
    [InlineData("http://api.anthropic.com/")]
    [InlineData("http://external-stub:8080/")]
    public void Resolve_OutsideE2E_WithNonHttpsValue_Throws(string configuredValue)
    {
        Assert.Throws<InvalidOperationException>(() => AnthropicBaseAddress.Resolve(configuredValue, "Production"));
    }

    [Fact]
    public void Resolve_OutsideE2E_WithRelativeValue_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => AnthropicBaseAddress.Resolve("not-a-uri", "Development"));
    }

    [Fact]
    public void Resolve_InsideE2E_WithExactSentinel_ReturnsSentinel()
    {
        var resolved = AnthropicBaseAddress.Resolve(AnthropicBaseAddress.E2ESentinel, "E2E");

        Assert.Equal(new Uri(AnthropicBaseAddress.E2ESentinel), resolved);
    }

    [Theory]
    [InlineData("https://api.anthropic.com/")]
    [InlineData("http://external-stub:8080")] // missing trailing slash — not an exact match
    [InlineData("http://evil.example.com:8080/")]
    public void Resolve_InsideE2E_WithAnythingOtherThanTheExactSentinel_Throws(string configuredValue)
    {
        Assert.Throws<InvalidOperationException>(() => AnthropicBaseAddress.Resolve(configuredValue, "E2E"));
    }

    [Fact]
    public void Resolve_InsideE2E_WithNoConfiguredValue_ThrowsRatherThanFallingBackToProductionDefault()
    {
        // An unset BaseUrl resolves to the real Anthropic API outside E2E — inside E2E that
        // fallback would silently defeat the whole point of the stub, so it must fail loudly
        // instead of ever resolving to a real provider host.
        Assert.Throws<InvalidOperationException>(() => AnthropicBaseAddress.Resolve(configuredValue: null, environmentName: "E2E"));
    }
}
