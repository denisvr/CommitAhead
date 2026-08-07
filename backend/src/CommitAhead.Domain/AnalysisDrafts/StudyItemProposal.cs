using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// A proposal to create a new StudyItem absent from the queue. Because AI cannot know the user's
/// Mastery, <see cref="AcceptedInitialMastery"/> is required on <see cref="Accept"/> even when
/// every other field is accepted unedited (model.md) — there is no AI-proposed mastery to default
/// to.
///
/// Field-level validation (title length, tag count, category/details match, mastery range) is
/// deliberately not duplicated here — it happens for real when the accepted fields are used to
/// construct the actual StudyItem at Apply time (Application layer), which is the aggregate that
/// owns those invariants. This type only guards its own required fields.
/// </summary>
public sealed class StudyItemProposal
{
    public Guid Id { get; }
    public ProposalStatus Status { get; private set; }
    public string ProposedTitle { get; }
    public StudyItemCategory ProposedCategory { get; }
    public StudyItemDetails ProposedDetails { get; }
    public IReadOnlyList<string> ProposedTags { get; }
    public int ProposedImportance { get; }

    public string? AcceptedTitle { get; private set; }
    public StudyItemCategory? AcceptedCategory { get; private set; }
    public StudyItemDetails? AcceptedDetails { get; private set; }
    public IReadOnlyList<string>? AcceptedTags { get; private set; }
    public int? AcceptedImportance { get; private set; }
    public int? AcceptedInitialMastery { get; private set; }

    public StudyItemProposal(
        Guid id,
        string proposedTitle,
        StudyItemCategory proposedCategory,
        StudyItemDetails proposedDetails,
        IReadOnlyList<string> proposedTags,
        int proposedImportance)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (string.IsNullOrWhiteSpace(proposedTitle))
        {
            throw new DomainValidationException("ProposedTitle is required.");
        }

        Id = id;
        ProposedTitle = proposedTitle;
        ProposedCategory = TextValidation.ValidateDefined(proposedCategory, nameof(proposedCategory));
        ProposedDetails = proposedDetails ?? throw new DomainValidationException("ProposedDetails is required.");
        ProposedTags = proposedTags ?? throw new DomainValidationException("ProposedTags is required.");
        ProposedImportance = proposedImportance;
        Status = ProposalStatus.Pending;
    }

    public void Accept(string title, StudyItemCategory category, StudyItemDetails details, IReadOnlyList<string> tags, int importance, int initialMastery)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("title is required.");
        }

        AcceptedTitle = title;
        AcceptedCategory = TextValidation.ValidateDefined(category, nameof(category));
        AcceptedDetails = details ?? throw new DomainValidationException("details is required.");
        AcceptedTags = tags ?? throw new DomainValidationException("tags is required.");
        AcceptedImportance = importance;
        AcceptedInitialMastery = initialMastery;
        Status = ProposalStatus.Accepted;
    }

    public void Reject()
    {
        EnsurePending();
        Status = ProposalStatus.Rejected;
    }

    private void EnsurePending()
    {
        if (Status != ProposalStatus.Pending)
        {
            throw new DomainValidationException("Only a Pending proposal can receive a decision.");
        }
    }
}
