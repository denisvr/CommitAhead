namespace CommitAhead.Domain.StudyItems;

public sealed class ScoringWeights
{
    public static ScoringWeights Default { get; } = new(40, 35, 25);

    public int ImportanceWeight { get; }
    public int DemandWeight { get; }
    public int MasteryGapWeight { get; }

    public ScoringWeights(int importanceWeight, int demandWeight, int masteryGapWeight)
    {
        if (importanceWeight < 0 || demandWeight < 0 || masteryGapWeight < 0)
        {
            throw new ArgumentException("Weights must be non-negative.");
        }

        if (importanceWeight + demandWeight + masteryGapWeight != 100)
        {
            throw new ArgumentException("Weights must sum to 100.");
        }

        ImportanceWeight = importanceWeight;
        DemandWeight = demandWeight;
        MasteryGapWeight = masteryGapWeight;
    }
}
