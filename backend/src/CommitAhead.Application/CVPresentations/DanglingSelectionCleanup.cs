using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// Invariant 25 ("deleting a canonical profile entry removes its CVPresentation selection rows")
/// has no DB-level FK to enforce it — CVPresentation's selections are plain uuid[] arrays (see
/// CVPresentation's own comment on why), so ProfessionalProfile's Replace* use cases call this
/// after a successful replace to strip any now-removed entry IDs from every affected
/// CVPresentation's matching selection collection, in the same transaction as the profile edit.
/// </summary>
internal static class DanglingSelectionCleanup
{
    public static async Task RemoveDanglingSelectionsAsync(
        ICVPresentationRepository cvPresentationRepository,
        Guid ownerUserId,
        IReadOnlySet<Guid> removedEntryIds,
        Func<CVPresentation, IReadOnlyList<Guid>> getSelections,
        Action<CVPresentation, IEnumerable<Guid>, DateTime> replaceSelections,
        CancellationToken cancellationToken)
    {
        if (removedEntryIds.Count == 0)
        {
            return;
        }

        var presentations = await cvPresentationRepository.GetAllAsync(ownerUserId, cancellationToken);
        var updatedAtUtc = DateTime.UtcNow;
        foreach (var presentation in presentations)
        {
            var selections = getSelections(presentation);
            if (selections.Any(removedEntryIds.Contains))
            {
                replaceSelections(presentation, selections.Where(entryId => !removedEntryIds.Contains(entryId)), updatedAtUtc);
            }
        }
    }
}
