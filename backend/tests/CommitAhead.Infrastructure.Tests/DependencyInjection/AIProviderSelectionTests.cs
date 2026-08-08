using CommitAhead.Application.AI;
using CommitAhead.Infrastructure.AI;
using CommitAhead.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Infrastructure.Tests.DependencyInjection;

/// <summary>ADR-0019's explicit, configuration-driven provider selection — one switch, evaluated once at composition-root time.</summary>
public class AIProviderSelectionTests
{
    private static IConfiguration BuildConfiguration(IDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:CommitAheadDb"] = "Host=localhost;Database=dummy;Username=dummy;Password=dummy",
        };
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddInfrastructure_WithProviderAnthropic_RegistersAnthropicAIProviderAsIAIProvider()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Anthropic",
            ["AI:Providers:Anthropic:ApiKey"] = "test-key",
            ["AI:Providers:Anthropic:Model"] = "claude-haiku-4-5-20251001",
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<AnthropicAIProvider>(provider.GetRequiredService<IAIProvider>());
    }

    [Fact]
    public void AddInfrastructure_WithAnUnknownProvider_ThrowsAtRegistrationTime()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AI:Provider"] = "OpenAI" });

        Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [Fact]
    public void AddInfrastructure_WithNoProviderConfigured_ThrowsAtRegistrationTime()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [Fact]
    public void AddInfrastructure_WithAnUnsupportedModel_ThrowsWhenTheProviderIsFirstResolved()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Anthropic",
            ["AI:Providers:Anthropic:ApiKey"] = "test-key",
            ["AI:Providers:Anthropic:Model"] = "claude-sonnet-not-yet-supported",
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IAIProvider>());
    }
}
