namespace CommitAhead.Infrastructure.AI;

/// <summary>
/// Thrown when an Anthropic call itself can't produce something usable — input-token ceiling
/// exceeded before any generation call, a provider timeout (distinct from caller cancellation),
/// refusal, missing/empty content, or an unexpected stop reason. Never carries the raw prompt,
/// response body, or provider error text — only a safe, fixed description. Malformed content that
/// parses far enough to be recognizably a response shape is AiResponseValidationException instead
/// (Application-layer — that one's already the right tool for "the AI's response isn't valid").
/// </summary>
public sealed class AiProviderException : Exception
{
    public AiProviderException(string message) : base(message)
    {
    }
}
