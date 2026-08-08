using System.Net;
using CommitAhead.Application.AI;
using CommitAhead.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>Zero real Anthropic calls (ADR-0009) — every test runs against RecordingHttpMessageHandler's canned responses.</summary>
public class AnthropicAIProviderTests
{
    private const string ApiKey = "test-anthropic-api-key";

    private static AnthropicAIProvider CreateProvider(RecordingHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        var options = Options.Create(new AnthropicOptions { ApiKey = ApiKey, Model = "claude-haiku-4-5-20251001" });
        return new AnthropicAIProvider(httpClient, options);
    }

    private static AiCallLimits DefaultLimits() => new(MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private static string EmptyStructuredOutputJson() => """{"suggestionProposals":[],"linkProposals":[],"studyItemProposals":[]}""";

    private static string MessagesResponse(string stopReason, string? text, int inputTokens = 50, int outputTokens = 20)
    {
        var content = text is null
            ? "[]"
            : "[{\"type\":\"text\",\"text\":" + System.Text.Json.JsonSerializer.Serialize(text) + "}]";
        return "{\"content\":" + content + ",\"stop_reason\":\"" + stopReason + "\",\"usage\":{\"input_tokens\":" + inputTokens + ",\"output_tokens\":" + outputTokens + "}}";
    }

    private static RecordingHttpMessageHandler HappyPathHandler(string structuredOutputJson = "", int inputTokens = 50, int outputTokens = 20)
    {
        var body = structuredOutputJson.Length == 0 ? EmptyStructuredOutputJson() : structuredOutputJson;
        return new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => JsonResponse(HttpStatusCode.OK, MessagesResponse("end_turn", body, inputTokens, outputTokens)),
            _ => throw new InvalidOperationException($"Unexpected request path: {request.RequestUri}"),
        });
    }

    [Fact]
    public void Describe_ReturnsTheConfiguredModelsProfile()
    {
        var provider = CreateProvider(new RecordingHttpMessageHandler((_, _) => throw new InvalidOperationException("Describe must not make a network call.")));

        var descriptor = provider.Describe(CommitAhead.Domain.AIUsage.AiCommandType.AnalyzeJobAnalysis);

        Assert.Equal("Anthropic", descriptor.Provider);
        Assert.Equal("claude-haiku-4-5-20251001", descriptor.Model);
        Assert.Equal("anthropic-haiku-4.5-2025-10", descriptor.PricingVersion);
        Assert.Equal("USD", descriptor.Currency);
        Assert.True(descriptor.EstimatedMaxCost > 0m);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_OnSuccess_ReturnsAnEmptyResultAndCallsCountTokensThenMessages()
    {
        var handler = HappyPathHandler();
        var provider = CreateProvider(handler);
        var input = new JobAnalysisAiInput("Job posting text.", [], [], []);

        var result = await provider.AnalyzeJobAnalysisAsync(input, DefaultLimits(), CancellationToken.None);

        Assert.Empty(result.SuggestionProposals);
        Assert.Empty(result.LinkProposals);
        Assert.Empty(result.StudyItemProposals);
        Assert.Equal(50, result.InputTokens);
        Assert.Equal(20, result.OutputTokens);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/v1/messages/count_tokens", handler.Requests[0].Request.RequestUri!.AbsolutePath);
        Assert.Equal("/v1/messages", handler.Requests[1].Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AnalyzeCVPresentationAsync_OnSuccess_ReturnsAnEmptyResult()
    {
        var provider = CreateProvider(HappyPathHandler());
        var input = new CVPresentationAiInput(null, [], [], [], []);

        var result = await provider.AnalyzeCVPresentationAsync(input, DefaultLimits(), CancellationToken.None);

        Assert.Empty(result.SuggestionProposals);
    }

    [Fact]
    public async Task AnalyzeInterviewNoteAsync_OnSuccess_ReturnsAnEmptyResult()
    {
        var provider = CreateProvider(HappyPathHandler());
        var input = new InterviewNoteAiInput("Acme", "Backend Engineer", "Technical", [], [], [], []);

        var result = await provider.AnalyzeInterviewNoteAsync(input, DefaultLimits(), CancellationToken.None);

        Assert.Empty(result.SuggestionProposals);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_SendsTheConfiguredModelAndAuthHeaders()
    {
        var handler = HappyPathHandler();
        var provider = CreateProvider(handler);

        await provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None);

        var messagesRequest = handler.Requests[1];
        Assert.Contains("\"model\":\"claude-haiku-4-5-20251001\"", messagesRequest.Body);
        Assert.Contains("\"output_config\"", messagesRequest.Body);
        Assert.Contains("\"json_schema\"", messagesRequest.Body);
        Assert.Equal(ApiKey, messagesRequest.Request.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", messagesRequest.Request.Headers.GetValues("anthropic-version").Single());
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_ComputesActualCostFromReportedUsageAndTheModelsPrices()
    {
        var provider = CreateProvider(HappyPathHandler(inputTokens: 1_000_000, outputTokens: 1_000_000));

        var result = await provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None);

        // Haiku 4.5: USD 1.00/1M input + USD 5.00/1M output (ADR-0019).
        Assert.Equal(6.00m, result.ActualCost);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WhenCountedInputTokensExceedTheLimit_ThrowsWithoutCallingMessages()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":9000}"""),
            _ => throw new InvalidOperationException("Must not call /v1/messages when the input-token limit is already exceeded."),
        });
        var provider = CreateProvider(handler);
        var limits = new AiCallLimits(MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), limits, CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WhenTheProviderReturnsANonSuccessStatus_ThrowsHttpRequestException()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => throw new InvalidOperationException("Unexpected path."),
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithMissingTextContent_ThrowsAiProviderException()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => JsonResponse(HttpStatusCode.OK, MessagesResponse("end_turn", null)),
            _ => throw new InvalidOperationException("Unexpected path."),
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithMalformedStructuredOutput_ThrowsAiResponseValidationException()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => JsonResponse(HttpStatusCode.OK, MessagesResponse("end_turn", "not valid json")),
            _ => throw new InvalidOperationException("Unexpected path."),
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<AiResponseValidationException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithARefusalStopReason_ThrowsAiProviderException()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => JsonResponse(HttpStatusCode.OK, MessagesResponse("refusal", EmptyStructuredOutputJson())),
            _ => throw new InvalidOperationException("Unexpected path."),
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithAMaxTokensStopReason_ThrowsAiProviderException_NotSilentlyParsingTruncatedOutput()
    {
        var handler = new RecordingHttpMessageHandler((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v1/messages/count_tokens" => JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}"""),
            "/v1/messages" => JsonResponse(HttpStatusCode.OK, MessagesResponse("max_tokens", """{"suggestionProposals":[],"linkPr""")),
            _ => throw new InvalidOperationException("Unexpected path."),
        });
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), DefaultLimits(), CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WhenTheProviderTimesOut_ThrowsAiProviderException_NotOperationCanceledException()
    {
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/v1/messages/count_tokens")
            {
                Thread.Sleep(200);
                return JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}""");
            }

            throw new InvalidOperationException("Unexpected path.");
        });
        var provider = CreateProvider(handler);
        var shortTimeoutLimits = new AiCallLimits(MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromMilliseconds(10));

        var exception = await Record.ExceptionAsync(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), shortTimeoutLimits, CancellationToken.None));

        Assert.IsType<AiProviderException>(exception);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WhenTheCallerCancelsBeforeTheAdapterTimeout_PropagatesCancellation_NotAiProviderException()
    {
        using var callerCts = new CancellationTokenSource();
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/v1/messages/count_tokens")
            {
                callerCts.Cancel();
                Thread.Sleep(50);
                return JsonResponse(HttpStatusCode.OK, """{"input_tokens":50}""");
            }

            throw new InvalidOperationException("Unexpected path.");
        });
        var provider = CreateProvider(handler);
        var longTimeoutLimits = new AiCallLimits(MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.AnalyzeJobAnalysisAsync(new JobAnalysisAiInput("Job posting text.", [], [], []), longTimeoutLimits, callerCts.Token));
    }
}
