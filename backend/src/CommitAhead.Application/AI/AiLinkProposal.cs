namespace CommitAhead.Application.AI;

/// <summary>A raw AI-proposed LinkProposal, before the analyzing use case validates TargetStudyItemId against the catalogue it sent and constructs the real Domain LinkProposal.</summary>
public sealed record AiLinkProposal(Guid TargetStudyItemId, decimal Weight, string Rationale);
