namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>Zero real network calls — records every request (not just the last) so a test can assert on both the count_tokens and messages calls a single AnthropicAIProvider call makes.</summary>
public sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _responder;

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public RecordingHttpMessageHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return _responder(request, body);
    }
}
