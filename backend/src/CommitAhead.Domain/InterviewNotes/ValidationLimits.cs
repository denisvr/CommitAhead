namespace CommitAhead.Domain.InterviewNotes;

/// <summary>Every length/count ceiling referenced by InterviewNote domain validation. A local copy, not shared with any other aggregate's <c>ValidationLimits</c>.</summary>
public static class ValidationLimits
{
    public const int ShortTextMaxLength = 200;
    public const int ListEntryMaxLength = 500;
    public const int MaxListEntryCount = 50;
}
