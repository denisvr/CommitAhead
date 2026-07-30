namespace CommitAhead.Application.StudyItems;

/// <summary>Shared outcome for mutations that only need to distinguish success from a missing/not-owned item.</summary>
public enum StudyItemMutationResult
{
    Success,
    NotFound,
}
