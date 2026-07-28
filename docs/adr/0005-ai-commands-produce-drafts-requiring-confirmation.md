---
status: accepted
date: 2026-07-28
---

# AI commands produce AnalysisDrafts; AI never writes to domain entities directly

## Context

Three AI commands are available: AnalyzeCVPresentation, AnalyzeJobAnalysis, and AnalyzeInterviewNote. Each reads an evidence source and produces proposals: suggestions for the source, links to existing StudyItems, and new StudyItems to create. The question was whether these proposals should be applied automatically or require explicit human review.

## Decision

Every AI command produces an `AnalysisDraft` containing three typed proposal collections: `SuggestionProposal[]`, `LinkProposal[]`, and `StudyItemProposal[]`. Each proposal carries its own `Pending | Accepted | Rejected` status. The draft itself transitions `Pending → Applied | Discarded`.

Applying a draft is an explicit user command. Only accepted proposals fan out to domain writes, atomically. Rejected proposals remain in the draft for audit. Only one Pending draft may exist per evidence source at a time.

AI never edits a source entity directly. Accepted `StructuredSuggestion`s fire normal domain commands; accepted `AdvisorySuggestion`s are marked for manual follow-up only.

## Consequences

- AI output is validated against schemas, IDs, lengths, enums, weights, and domain invariants before the draft is created — malformed proposals are rejected at the boundary.
- A user who triggers an analysis and immediately closes the app will find the draft waiting on next login.
- The "one Pending draft per source" invariant prevents re-triggering analysis while a previous draft is unreviewed, which also limits accidental AI cost duplication.

## Considered Alternatives

Auto-applying AI proposals without confirmation would require the system to trust that AI-generated IDs reference valid StudyItems, that weights are in range, and that proposal content is free of prompt injection artefacts. Given that AI output is treated as untrusted input throughout the system, auto-application would violate the trust boundary established elsewhere.
