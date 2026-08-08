namespace CommitAhead.Application.StudyItems;

/// <summary>
/// Thrown by <see cref="StudyItemDetailsJsonParser"/> when a category+JSON pair fails to parse or
/// validate. Deliberately neutral — carries no assumption about who's calling (an AI-output
/// validator wraps this as <c>AiResponseValidationException</c>; ApplyAnalysisDraftUseCase wraps it
/// as <c>ApplyAnalysisDraftValidationException</c>). Never carries the raw JSON.
/// </summary>
public sealed class StudyItemDetailsPayloadException : Exception
{
    public StudyItemDetailsPayloadException(string message) : base(message)
    {
    }
}
