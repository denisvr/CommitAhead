---
status: accepted
date: 2026-07-28
---

# Evidence-source deletion and polymorphic cleanup are application-managed

**Status: superseded — this feature was removed from the app (see docs/roadmap.md). `CVPresentation`'s delete is now a plain single-aggregate delete; the polymorphic EvidenceLink/AnalysisDraft cleanup this ADR describes no longer exists. Kept for historical record.**

## Context

`EvidenceLink` and `AnalysisDraft` carry polymorphic source references: `sourceType` (CVPresentation, JobAnalysis, or InterviewNote) plus `sourceId`. Normal foreign-key cascades cannot target three possible tables. Uploaded JobAnalysis sources also create a cross-system Storage cleanup problem.

## Decision

**Database cascade**: A normal `ON DELETE CASCADE` foreign key is impossible across a polymorphic reference pointing to three different tables. Source deletion is handled by an explicit application-managed transaction:
1. Delete all `EvidenceLink` rows for the source.
2. Delete all `AnalysisDraft` rows and their proposal children for the source, including a Pending draft if one exists.
3. Delete the source entity.
All steps occur within a single database transaction. Demand on affected `StudyItem`s is recomputed on the next ranked-list query.

`AIUsageRecord` rows are retained because they contain cost/audit metadata only and no source content. Their source ID may therefore refer to a deleted source.

**Supabase Storage file cleanup**: When a `JobAnalysis` with an `UploadedFile` source is deleted, its Storage object must also be deleted. Storage and the PostgreSQL database do not share a transaction boundary, so file deletion cannot be atomic with the DB delete. The operation order is:
1. Commit the database transaction (EvidenceLinks deleted, source deleted).
2. Attempt to delete the Storage object after the transaction commits.
3. If Storage deletion fails, log the orphaned `storageObjectKey` for manual cleanup. The DB record is already gone; the orphaned file is the accepted failure mode.

This is an eventually-consistent best-effort cleanup. A scheduled orphan-cleanup job is deferred to post-MVP.

## Consequences

- The deletion guard — a `StudyItem` cannot be hard-deleted while EvidenceLinks point to it — is enforced at two levels: the use case checks before deleting, and a database FK from `EvidenceLink.targetStudyItemId` to `StudyItem.id` (no cascade) ensures the DB rejects any missed case.
- All source-deletion use cases must explicitly delete EvidenceLinks and AnalysisDrafts within the transaction before deleting the source; this cannot be handled by a normal polymorphic FK cascade.
- Storage orphan risk is real but bounded: only `UploadedFile` sources are affected, and orphaned objects cost negligible Storage space.

## Considered Alternatives

A separate `EvidenceLink` table per source type (e.g. `JobAnalysisEvidenceLinks`) would allow normal FK cascades but would duplicate the EvidenceLink schema, complicate the query for `StudyItem` demand computation, and require schema changes whenever a new source type is added.
