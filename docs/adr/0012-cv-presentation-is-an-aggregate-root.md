---
status: accepted
date: 2026-07-28
---

# CVPresentation is an independent aggregate root

## Context

CVPresentation was initially modelled as a child collection inside the singleton ProfessionalProfile. It later gained an independent lifecycle: direct create/update/delete use cases, export, AI analysis, AnalysisDraft references, and EvidenceLinks. Other aggregates therefore needed to reference a CVPresentation ID directly.

An aggregate-internal entity should not be an externally referenced consistency boundary. Loading the entire ProfessionalProfile to analyse, export, or update one presentation would also make the profile aggregate unnecessarily broad.

## Decision

`CVPresentation` is an aggregate root with its own ID and `professionalProfileId` reference. `ProfessionalProfile` remains the canonical owner of Experience, Education, Skill, Language, Certification, Project, ProfileLink, and ContactInfo data.

CVPresentation stores ordered selections referencing those canonical entries by ID. It never duplicates their content. Use cases validate that every selected entry belongs to the referenced ProfessionalProfile.

## Consequences

- AnalysisDraft and EvidenceLink can reference CVPresentation as a normal aggregate root.
- CV editing/export does not require treating the full ProfessionalProfile as the transaction boundary.
- Deleting a ProfessionalProfile is not an MVP use case. If added later, it must explicitly handle dependent CVPresentations.
- Ordered selection join tables provide FKs to both CVPresentation and the selected canonical entry; the same-profile invariant remains application-enforced.
- Deleting a canonical entry cascades only its selection rows. It does not delete or duplicate a CVPresentation.

## Considered Alternatives

Keeping CVPresentation inside ProfessionalProfile would preserve a single write boundary, but external references to its child IDs would violate that boundary and make independent presentation operations awkward. Copying canonical entries into each presentation was rejected because it introduces version drift and contradicts the canonical-profile model.
