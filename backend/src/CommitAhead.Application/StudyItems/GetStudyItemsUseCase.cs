using CommitAhead.Application.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

/// <summary>
/// The Study Items list view (Active + Archived, not just the ranked Active-only queue). No
/// score fields — those require ScoringConfig/EvidenceLink queries that only the ranked queue
/// (GetRankedStudyQueueUseCase) and the detail view (GetStudyItemUseCase) need.
/// </summary>
public sealed class GetStudyItemsUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetStudyItemsUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// status is a raw, optional query string, not StudyItemStatus, so the enum stays out of the
    /// controller's own signature (NetArchTest rule 4 — controllers must not depend on Domain).
    /// </summary>
    public async Task<IReadOnlyList<StudyItemSummary>> ExecuteAsync(string? status, CancellationToken cancellationToken)
    {
        var parsedStatus = ParseStatus(status);
        var items = await _repository.GetAllAsync(_currentUser.UserId, cancellationToken);

        return items
            .Where(item => parsedStatus is null || item.Status == parsedStatus)
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(item => new StudyItemSummary(item.Id, item.Title, item.Category, item.Status, item.Importance, item.CreatedAtUtc, item.UpdatedAtUtc))
            .ToList();
    }

    private static StudyItemStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<StudyItemStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new DomainValidationException($"status must be one of: {string.Join(", ", Enum.GetNames<StudyItemStatus>())}.");
        }

        return parsed;
    }
}

public sealed record StudyItemSummary(
    Guid Id,
    string Title,
    StudyItemCategory Category,
    StudyItemStatus Status,
    int Importance,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
