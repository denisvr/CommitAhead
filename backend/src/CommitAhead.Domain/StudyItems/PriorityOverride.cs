using CommitAhead.Domain;

namespace CommitAhead.Domain.StudyItems;

public sealed class PriorityOverride
{
    public int Score { get; }
    public string Reason { get; }

    public PriorityOverride(int score, string reason)
    {
        if (score is < 0 or > 100)
        {
            throw new DomainValidationException("Score must be in [0,100].");
        }

        Score = score;
        Reason = TextValidation.RequireNonBlank(reason, nameof(reason), ValidationLimits.PriorityOverrideReasonMaxLength);
    }
}
