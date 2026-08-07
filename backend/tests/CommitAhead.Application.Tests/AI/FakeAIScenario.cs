namespace CommitAhead.Application.Tests.AI;

/// <summary>The six deterministic scenarios ADR-0009 requires FakeAIProvider to simulate, for every one of the three analyze commands.</summary>
public enum FakeAIScenario
{
    /// <summary>One proposal of each kind, all individually valid.</summary>
    Success,

    /// <summary>No proposals at all — a real, but empty, AI response.</summary>
    EmptyOutput,

    /// <summary>Proposals with data an analyzing use case must reject (an out-of-range weight, an undefined enum).</summary>
    MalformedProposals,

    /// <summary>The same LinkProposal.TargetStudyItemId proposed twice.</summary>
    Duplicates,

    /// <summary>Simulates the provider call exceeding its time budget.</summary>
    Timeout,

    /// <summary>Simulates a genuine provider-side failure (unreachable, 5xx, etc.) — an infrastructure error, not a validation problem.</summary>
    ProviderFailure,
}
