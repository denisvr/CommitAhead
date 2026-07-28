---
status: accepted
date: 2026-07-28
---

# EvidenceLink source deletion is managed by the application, not the database

## Context

`EvidenceLink` carries a polymorphic source reference: `sourceType` (CVPresentation, JobAnalysis, or InterviewNote) plus `sourceId`. This pattern is required because EvidenceLinks can point to three different source types. It creates two related problems: cascade deletion and Storage file cleanup.

## Decision

**Database cascade**: A normal `ON DELETE CASCADE` foreign key is impossible across a polymorphic reference pointing to three different tables. Source deletion is handled by an explicit application-managed transaction:
1. Delete all `EvidenceLink` rows for the source.
2. Delete the source entity.
Both steps occur within a single database transaction. Demand on affected `StudyItem`s is recomputed on the next ranked-list query.

**Supabase Storage file cleanup**: When a `JobAnalysis` with an `UploadedFile` source is deleted, its Storage object must also be deleted. Storage and the PostgreSQL database do not share a transaction boundary, so file deletion cannot be atomic with the DB delete. The operation order is:
1. Commit the database transaction (EvidenceLinks deleted, source deleted).
2. Attempt to delete the Storage object after the transaction commits.
3. If Storage deletion fails, log the orphaned `storageObjectKey` for manual cleanup. The DB record is already gone; the orphaned file is the accepted failure mode.

This is an eventually-consistent best-effort cleanup. A scheduled orphan-cleanup job is deferred to post-MVP.

## Consequences

- The deletion guard — a `StudyItem` cannot be hard-deleted while EvidenceLinks point to it — is enforced at two levels: the use case checks before deleting, and a database FK from `EvidenceLink.targetStudyItemId` to `StudyItem.id` (no cascade) ensures the DB rejects any missed case.
- All source-deletion use cases must explicitly load and delete EvidenceLinks within the transaction before deleting the source; this cannot be handled by the ORM automatically.
- Storage orphan risk is real but bounded: only `UploadedFile` sources are affected, and orphaned objects cost negligible Storage space.

## Considered Alternatives

A separate `EvidenceLink` table per source type (e.g. `JobAnalysisEvidenceLinks`) would allow normal FK cascades but would duplicate the EvidenceLink schema, complicate the query for `StudyItem` demand computation, and require schema changes whenever a new source type is added.
