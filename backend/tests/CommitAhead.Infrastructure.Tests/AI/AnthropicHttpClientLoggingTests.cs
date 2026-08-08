using System.Net;
using CommitAhead.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>
/// Proves the "AnthropicAIProvider" named HttpClient genuinely redacts its x-api-key header via
/// HttpClientFactoryOptions.ShouldRedactHeaderValue — not merely that Trace-level HTTP logging
/// happens to be disabled by the Warning filter InfrastructureServiceCollectionExtensions also
/// configures. This test explicitly re-enables Trace for the same category so header logging
/// actually runs, then asserts the raw key never appears in any captured message.
/// </summary>
public class AnthropicHttpClientLoggingTests
{
    private const string RawApiKey = "super-secret-anthropic-key-value";

    [Fact]
    public async Task AnthropicNamedHttpClient_NeverLogsTheRawApiKeyHeaderValue_EvenAtTraceLogging()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CommitAheadDb"] = "Host=localhost;Database=dummy;Username=dummy;Password=dummy",
            ["AI:Provider"] = "Anthropic",
            ["AI:Providers:Anthropic:ApiKey"] = RawApiKey,
            ["AI:Providers:Anthropic:Model"] = "claude-haiku-4-5-20251001",
        }).Build();

        services.AddInfrastructure(configuration);

        // No real network call — a stub primary handler for the same named client.
        services.AddHttpClient("AnthropicAIProvider")
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)));

        var loggerProvider = new CapturingLoggerProvider();
        services.AddLogging(logging =>
        {
            logging.AddProvider(loggerProvider);
            // Overrides InfrastructureServiceCollectionExtensions' own Warning filter for this exact
            // category — deliberately, so header logging actually fires and redaction is genuinely
            // exercised, not just skipped because the level was too low to log anything at all.
            logging.AddFilter("System.Net.Http.HttpClient.AnthropicAIProvider", LogLevel.Trace);
        });

        using var provider = services.BuildServiceProvider();
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("AnthropicAIProvider");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages") { Content = new StringContent("{}") };
        using var response = await httpClient.SendAsync(request);

        var messages = loggerProvider.Messages.ToList();
        Assert.DoesNotContain(messages, message => message.Contains(RawApiKey, StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("x-api-key", StringComparison.OrdinalIgnoreCase));
    }
}
