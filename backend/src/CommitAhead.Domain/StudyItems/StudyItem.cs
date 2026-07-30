namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// The primary unit of preparation and the only entity ranked in the study queue, scoped to one
/// user (ADR-0015). Category-specific structure lives in Details (ADR-0001); Mastery, Demand and
/// EffectiveScore are never stored here (ADR-0003) — ComputeMastery is the one exception the
/// aggregate itself can answer without a query, since it only needs its own loaded Reviews.
/// </summary>
public sealed class StudyItem
{
    private readonly List<StudyReview> _reviews = [];

    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string Title { get; private set; }
    public StudyItemCategory Category { get; }
    public StudyItemStatus Status { get; private set; }
    public int Importance { get; private set; }
    public int InitialMastery { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; }
    public StudyItemDetails Details { get; private set; }
    public PriorityOverride? PriorityOverride { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyList<StudyReview> Reviews => _reviews;

    /// <summary>
    /// The StudyReview half of invariant 2's hard-delete guard. EvidenceLink is a separate
    /// aggregate this entity has no reference to, so DeleteStudyItemUseCase checks that half via
    /// IEvidenceLinkQuery instead of here.
    /// </summary>
    public bool CanBeHardDeleted => _reviews.Count == 0;

    public StudyItem(
        Guid id,
        Guid ownerUserId,
        string title,
        StudyItemCategory category,
        int importance,
        int initialMastery,
        IReadOnlyList<string> tags,
        StudyItemDetails details,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        }

        Id = id;
        OwnerUserId = ownerUserId;
        Category = category;
        Title = ValidateTitle(title);
        Importance = ValidateRating(importance, nameof(importance));
        InitialMastery = ValidateRating(initialMastery, nameof(initialMastery));
        Tags = TagNormalizer.Normalize(tags);
        Details = ValidateDetails(category, details);
        Status = StudyItemStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void Update(string title, int importance, IEnumerable<string> tags, StudyItemDetails details, DateTime updatedAtUtc)
    {
        Title = ValidateTitle(title);
        Importance = ValidateRating(importance, nameof(importance));
        Tags = TagNormalizer.Normalize(tags);
        Details = ValidateDetails(Category, details);
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Purely a manual status flip — Mastery never archives an item automatically (invariant 1).</summary>
    public void Archive(DateTime updatedAtUtc)
    {
        Status = StudyItemStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AddReview(StudyReview review, DateTime updatedAtUtc)
    {
        _reviews.Add(review);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetPriorityOverride(PriorityOverride priorityOverride, DateTime updatedAtUtc)
    {
        PriorityOverride = priorityOverride;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ClearPriorityOverride(DateTime updatedAtUtc)
    {
        PriorityOverride = null;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>InitialMastery until the first review; otherwise the average of the three most recent ratings (docs/domain/model.md).</summary>
    public decimal ComputeMastery()
    {
        if (_reviews.Count == 0)
        {
            return InitialMastery;
        }

        var mostRecent = _reviews
            .OrderByDescending(review => review.ReviewedAtUtc)
            .ThenByDescending(review => review.Id)
            .Take(3);

        return mostRecent.Average(review => (decimal)review.ConfidenceRating);
    }

    private static StudyItemDetails ValidateDetails(StudyItemCategory category, StudyItemDetails details)
    {
        var matches = category switch
        {
            StudyItemCategory.LeetCode => details is LeetCodeDetails,
            StudyItemCategory.SystemDesign => details is SystemDesignDetails,
            StudyItemCategory.Behavioral => details is BehavioralDetails,
            StudyItemCategory.Theory => details is TheoryDetails,
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };

        if (!matches)
        {
            throw new ArgumentException($"Details type {details.GetType().Name} does not match category {category}.", nameof(details));
        }

        return details;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return title.Trim();
    }

    private static int ValidateRating(int value, string paramName)
    {
        if (value is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rating must be in [1,5].");
        }

        return value;
    }
}
