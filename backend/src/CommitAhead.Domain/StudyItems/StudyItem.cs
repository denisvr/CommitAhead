using CommitAhead.Domain;

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
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new DomainValidationException("Category is not a recognized value.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        Category = category;
        Title = ValidateTitle(title);
        Importance = ValidateRating(importance, nameof(importance));
        InitialMastery = ValidateRating(initialMastery, nameof(initialMastery));
        Tags = ValidateTags(tags);
        Details = ValidateDetails(category, details);
        Status = StudyItemStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void Update(string title, int importance, IEnumerable<string> tags, StudyItemDetails details, DateTime updatedAtUtc)
    {
        Title = ValidateTitle(title);
        Importance = ValidateRating(importance, nameof(importance));
        Tags = ValidateTags(tags);
        Details = ValidateDetails(Category, details);
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Purely a manual status flip — Mastery never archives an item automatically (invariant 1).</summary>
    public void Archive(DateTime updatedAtUtc)
    {
        Status = StudyItemStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>The reverse of Archive — also purely user-initiated (invariant 1).</summary>
    public void Restore(DateTime updatedAtUtc)
    {
        Status = StudyItemStatus.Active;
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
            _ => throw new DomainValidationException("Category is not a recognized value."),
        };

        if (!matches)
        {
            throw new DomainValidationException($"Details type {details.GetType().Name} does not match category {category}.");
        }

        return details;
    }

    private static string ValidateTitle(string title) => TextValidation.RequireNonBlank(title, nameof(title), ValidationLimits.TitleMaxLength);

    private static IReadOnlyList<string> ValidateTags(IEnumerable<string> tags)
    {
        var normalized = TagNormalizer.Normalize(tags);
        if (normalized.Count > ValidationLimits.MaxTagCount)
        {
            throw new DomainValidationException($"Tags must have at most {ValidationLimits.MaxTagCount} entries.");
        }

        if (normalized.Any(tag => tag.Length > ValidationLimits.TagMaxLength))
        {
            throw new DomainValidationException($"Each tag must be at most {ValidationLimits.TagMaxLength} characters.");
        }

        return normalized;
    }

    private static int ValidateRating(int value, string paramName)
    {
        if (value is < 1 or > 5)
        {
            throw new DomainValidationException($"{paramName} must be in [1,5].");
        }

        return value;
    }
}
