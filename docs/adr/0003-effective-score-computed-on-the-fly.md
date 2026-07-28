---
status: accepted
date: 2026-07-28
---

# EffectiveScore computed on-the-fly; no denormalisation for MVP

## Context

The study queue must rank StudyItems by EffectiveScore. Score depends on three inputs: Importance (persisted), Mastery (derived from recent StudyReviews), and Demand (derived from confirmed EvidenceLink weights). The question was whether to persist the computed score or derive it at query time.

## Decision

Mastery, Demand, and EffectiveScore are not persisted on `StudyItem`. The ranked-list query computes them inline:
- **Mastery**: `InitialMastery` until the first `StudyReview` exists; thereafter the average of the three most recent confidence ratings.
- **Demand**: `min(Σ confirmed EvidenceLink weights pointing to this item, 5)`.
- **EffectiveScore**: `(Importance/5)×40 + (Demand/5)×35 + ((5−Mastery)/4)×25`, or `PriorityOverride.score` when set.

Denormalisation is deferred until a performance measurement shows it is necessary.

## Consequences

- `StudyItem` persists only source facts: `importance`, `initialMastery`, and `priorityOverride`. No sync triggers are needed.
- The ranked-list query joins `StudyReview` and `EvidenceLink` rows and cannot use a simple indexed sort on a stored score column — this is the accepted MVP trade-off.
- Minimum computed score is 8 (when `importance=1`, `demand=0`, `mastery=5`). A `PriorityOverride` can set any value in `[0, 100]`.
- Tiebreaking order for items with equal EffectiveScore is **TBD** (see `docs/tbd.md`).

## Considered Alternatives

Persisting a denormalised `score` column would require explicit recomputation triggers on every input change (review submitted, link added or removed, weights reconfigured). For an invite-only app with a small number of users and low write frequency, this adds synchronisation complexity for no measured benefit.
