# EvidenceLink source deletion is managed by the application, not the database

`EvidenceLink` carries a polymorphic source reference: `sourceType` (CVPresentation, JobAnalysis, or InterviewNote) plus `sourceId`. A normal database foreign key with `ON DELETE CASCADE` is impossible across a polymorphic reference pointing to three different tables.

Source deletion is therefore handled by an explicit application-managed transaction: the use case deletes all `EvidenceLink` rows for the source first, then deletes the source entity, within a single database transaction. Demand on affected `StudyItem`s is recomputed on the next ranked-list query.

**Consequence**: the deletion guard — a `StudyItem` cannot be hard-deleted while EvidenceLinks point to it — must also be enforced by the application (use case checks before delete) and backed by a database FK from `EvidenceLink.targetStudyItemId` to `StudyItem.id` with no cascade, so the DB will reject any attempt to delete a referenced item that the application missed.
