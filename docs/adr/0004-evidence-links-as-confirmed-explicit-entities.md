---
status: superseded
date: 2026-07-28
---

# EvidenceLinks are explicit confirmed entities, not automatic tag-matching

**Status: superseded — this feature was removed from the app (see docs/roadmap.md). Kept for historical record.**

## Context

Demand — one of the three inputs to EffectiveScore — must reflect how urgently evidence sources signal that a StudyItem topic is needed. The design question was how to compute that signal: automatically from shared tags, or via explicit human-confirmed links.

## Decision

`EvidenceLink` is an explicit domain entity. It is created only from an accepted `LinkProposal` inside an `AnalysisDraft`. Each link carries a `weight` (0–5) and a `rationale` visible in the UI. `Demand` is `min(Σ confirmed EvidenceLink weights, 5)`.

At most one `EvidenceLink` may exist per `(sourceType, sourceId, targetStudyItemId)` pair (enforced by a unique database constraint). Tags remain in the model for organisation and filtering only — they do not contribute to Demand.

## Consequences

- Creating an EvidenceLink requires triggering an AI analysis command and confirming the resulting `LinkProposal`. There is no automatic or direct/manual creation command.
- Demand is fully traceable: each point of demand is tied to a specific evidence source, weight, and human-confirmed rationale.
- The UI can display "why is this item prioritised?" by listing its confirmed EvidenceLinks.

## Considered Alternatives

Tag-based automatic demand: if a StudyItem and a JobAnalysis share a tag (e.g. `"kafka"`), demand increases automatically. This was rejected because tag counts accumulate invisibly, are sensitive to normalisation inconsistencies, and provide no rationale. A user cannot tell which evidence sources are driving demand or why — making the priority score opaque despite its transparent formula.
