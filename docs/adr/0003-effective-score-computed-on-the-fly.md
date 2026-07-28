# EffectiveScore computed on-the-fly; no denormalisation for MVP

Mastery, Demand, and EffectiveScore are not persisted on `StudyItem`. The ranked-list query computes them inline from source facts: `StudyReview` confidence ratings and confirmed `EvidenceLink` weights. Denormalising these values would require explicit recomputation triggers on every input change (review submitted, link added or removed, weights reconfigured), adding synchronisation complexity for no measured gain. Write frequency for a single-user app is low enough that on-the-fly computation is sufficient. Denormalisation is deferred until a performance measurement shows it is necessary.

**Consequence**: the ranked-list query joins `StudyReview` and `EvidenceLink` rows and cannot use a simple indexed sort on a stored score column. This is the accepted trade-off for MVP.
