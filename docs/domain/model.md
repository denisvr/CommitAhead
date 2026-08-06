# CommitAhead — Domain Model

For term definitions see `CONTEXT.md`. For architectural decisions see `docs/adr/`.

---

## Aggregate Roots

Every aggregate root below carries an `ownerUserId` (see ADR-0015): all reads, writes, and cross-aggregate references are scoped to the authenticated request's owner. There is no cross-user sharing — a user's data is invisible to every other user, full stop.

### 1. StudyItem

The primary unit of preparation and the only entity ranked in the study queue, scoped to one user.

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `ownerUserId` | UUID | FK to the owning User (ADR-0015); never exposed to other users |
| `title` | string | Canonical name; not duplicated in details |
| `category` | enum | Theory \| LeetCode \| SystemDesign \| Behavioral |
| `status` | enum | Active \| Archived |
| `importance` | int 1–5 | Manual; never computed |
| `initialMastery` | int 1–5 | Used until the first StudyReview |
| `tags` | string[] | Trimmed, lowercase, kebab-case, unique per item |
| `details` | StudyItemDetails | Discriminated union matching `category` |
| `priorityOverride` | PriorityOverride? | Null means use computed score |
| `createdAt` | timestamp | UTC |
| `updatedAt` | timestamp | UTC |

**Canonical tags:** normalisation is a fixed trim/lowercase/kebab-case transform, not a synonym
table — it has no way to know that "C#" and "C Sharp" name the same language, so distinct spellings
of an ambiguous technical term normalise to distinct tags. Enter (or expect an AI proposal to
enter) the spelled-out form so normalisation lands on the conventional tag:

| Term | Type this | Normalises to |
|---|---|---|
| C# | `C Sharp` | `c-sharp` |
| C++ | `C Plus Plus` | `c-plus-plus` |
| .NET | `dotnet` | `dotnet` |

**Children:** `StudyReview[]`

#### StudyReview

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | Unique inside the aggregate |
| `reviewedAt` | timestamp | UTC; determines recency |
| `confidenceRating` | int 1–5 | |
| `notesMarkdown` | string? | Optional private review notes |

#### Computed query fields

| Field | Formula |
|---|---|
| `mastery` | `initialMastery` before the first review; otherwise the average of up to three most recent ratings ordered by `reviewedAt DESC, id DESC` |
| `demand` | `min(Σ EvidenceLink.weight for existing links targeting the item, 5)` |
| `effectiveScore` | `(importance/5)×importanceWeight + (demand/5)×demandWeight + ((5−mastery)/4)×masteryGapWeight`, or `priorityOverride.score` |

The default weights are 40/35/25. The ranked queue orders by `EffectiveScore DESC, CreatedAt ASC, Id ASC` — see `docs/architecture/persistence.md` ("Ranked-list ordering").

---

### 2. ProfessionalProfile *(singleton per user)*

The canonical source of professional identity and reusable CV content — one per user, not a single global row. A user's first save creates their row; there is never more than one per `ownerUserId`.

| Field | Type |
|---|---|
| `id` | UUID |
| `ownerUserId` | UUID |
| `contactInfo` | ContactInfo |
| `summaryMarkdown` | string |
| `createdAt` | timestamp |
| `updatedAt` | timestamp |

**Child entity collections:**

#### ExperienceEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `company` | string |
| `client` | string? |
| `role` | string |
| `employmentType` | Permanent \| Contract \| Freelance \| Internship \| Other |
| `startDate` | YearMonth |
| `endDate` | YearMonth? (`null` = current) |
| `location` | string? |
| `workMode` | OnSite \| Hybrid \| Remote \| Other |
| `summaryMarkdown` | string |
| `achievements` | string[] |
| `skillIds` | UUID[] referencing Skills in the same profile |

#### EducationEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `institution` | string |
| `degree` | string |
| `field` | string? |
| `startDate` | YearMonth? |
| `endDate` | YearMonth? |
| `location` | string? |
| `detailsMarkdown` | string? |

#### Skill

| Field | Type |
|---|---|
| `id` | UUID |
| `displayName` | string |
| `normalizedKey` | string | Trimmed, lowercase, kebab-case; unique inside the profile |
| `category` | SkillCategory |
| `proficiency` | Beginner \| Intermediate \| Advanced \| Expert \| null |

`SkillCategory`: Language, Framework, Platform, Cloud, Database, Messaging, DevOps, Testing, Architecture, Tool, Methodology, Domain, Other.

#### LanguageEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `language` | string |
| `proficiency` | A1 \| A2 \| B1 \| B2 \| C1 \| C2 \| Native |
| `certification` | string? |

#### CertificationEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `name` | string |
| `issuingOrganisation` | string |
| `issuedAt` | YearMonth? |
| `expiresAt` | YearMonth? |
| `credentialId` | string? |
| `url` | absolute http/https URL? |

#### ProjectEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `name` | string |
| `role` | string? |
| `startDate` | YearMonth? |
| `endDate` | YearMonth? |
| `descriptionMarkdown` | string |
| `url` | absolute http/https URL? |
| `skillIds` | UUID[] referencing Skills in the same profile |

#### ProfileLink

| Field | Type |
|---|---|
| `id` | UUID |
| `kind` | GitHub \| LinkedIn \| Portfolio \| Blog \| Other |
| `label` | string? |
| `url` | absolute http/https URL |

CVPresentations are not children of ProfessionalProfile. They are independent aggregate roots because they have their own lifecycle, AI analyses, EvidenceLinks, and export use cases. See ADR-0012.

---

### 3. CVPresentation

A curated, locale-specific projection over one ProfessionalProfile.

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `ownerUserId` | UUID | FK to the owning User (ADR-0015) |
| `professionalProfileId` | UUID | FK to the user's own ProfessionalProfile — must share `ownerUserId` |
| `label` | string | e.g. “UK — Senior Backend Engineer” |
| `targetMarket` | string | Country/market identifier |
| `targetRole` | string? | |
| `locale` | string | BCP 47 locale |
| `templateKey` | string | References an available export template |
| `summaryOverrideMarkdown` | string? | Null uses profile summary |
| `includePhoto` | bool | |
| `includeEmail` | bool | |
| `includePhone` | bool | |
| `includeAddress` | bool | |
| `dateFormat` | string | Locale-aware rendering rule |
| `pageLimit` | int > 0 | |
| `createdAt` | timestamp | UTC |
| `updatedAt` | timestamp | UTC |

It owns seven ordered selection collections: Experience, Education, Skill, Language, Certification, Project, and ProfileLink. Each selection is an ordered list of canonical entry IDs — list order *is* position (invariant 24); there is no separate stored `position` value alongside each ID (see ADR-0017 for the persistence shape). ProfileLinks default to all existing links when a presentation is created but can be explicitly excluded afterward.

---

### 4. JobAnalysis

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `ownerUserId` | UUID | FK to the owning User (ADR-0015) |
| `title` | string | User-assigned label |
| `jobSource` | JobSource | PastedText or UploadedFile |
| `requirements` | JobRequirement[] | Child entities |
| `gaps` | JobGap[] | Child entities |
| `notesMarkdown` | string? | |
| `createdAt` | timestamp | UTC |
| `updatedAt` | timestamp | UTC |

#### JobRequirement *(child entity)*

| Field | Type |
|---|---|
| `id` | UUID |
| `text` | string |
| `kind` | Technical \| Behavioural \| Experience \| Domain \| Language \| Other |
| `priority` | Required \| Preferred |
| `sourceExcerpt` | string |

#### JobGap *(child entity)*

| Field | Type |
|---|---|
| `id` | UUID |
| `requirementId` | UUID referencing a requirement in the same JobAnalysis |
| `matchLevel` | Partial \| Missing \| Unknown |
| `severity` | High \| Medium \| Low |
| `rationale` | string |

---

### 5. InterviewNote

| Field | Type |
|---|---|
| `id` | UUID |
| `ownerUserId` | UUID |
| `company` | string |
| `role` | string |
| `interviewRound` | InterviewRound |
| `sequenceNumber` | int > 0 |
| `otherLabel` | string? |
| `date` | date |
| `questions` | string[] |
| `gaps` | string[] |
| `lessons` | string[] |
| `jobAnalysisId` | UUID? |
| `createdAt` | timestamp |
| `updatedAt` | timestamp |

`InterviewRound`: RecruiterScreening, HiringManager, Technical, LiveCoding, TakeHome, SystemDesign, Behavioral, Panel, Final, Other.

---

### 6. AnalysisDraft

| Field | Type |
|---|---|
| `id` | UUID |
| `ownerUserId` | UUID |
| `sourceType` | CVPresentation \| JobAnalysis \| InterviewNote |
| `sourceId` | UUID — must reference a source with the same `ownerUserId` |
| `status` | Pending \| Applied \| Discarded |
| `createdAt` | timestamp |
| `appliedAt` | timestamp? |
| `discardedAt` | timestamp? |

**Child collections:** `SuggestionProposal[]`, `LinkProposal[]`, `StudyItemProposal[]`.

Every proposal has `id`, final `status` (`Pending | Accepted | Rejected`), an immutable AI `proposedPayload`, and an optional `acceptedPayload` stored separately for audit:

- `SuggestionProposal`: either `StructuredSuggestion(commandType, payload)` or `AdvisorySuggestion(markdown)`. The supported structured command allowlist is a Phase 4 decision in `docs/tbd.md`.
- `LinkProposal`: `targetStudyItemId`, `weight`, and `rationale`.
- `StudyItemProposal`: proposed `title`, `category`, typed details, tags, and importance. Because AI cannot know the user's mastery, an accepted decision must provide a user-selected `initialMastery`.

`ApplyAnalysisDraft` receives exactly one `ProposalDecision` for every proposal. Every accepted actionable decision includes its complete user-finalised payload; an accepted AdvisorySuggestion requires no separate payload because it has no automatic effect. Rejected decisions have no accepted payload. All decisions, final payloads, accepted effects, and the Applied status are committed atomically.

---

### 7. EvidenceLink

| Field | Type |
|---|---|
| `id` | UUID |
| `ownerUserId` | UUID |
| `sourceType` | CVPresentation \| JobAnalysis \| InterviewNote |
| `sourceId` | UUID — must reference a source with the same `ownerUserId` |
| `targetStudyItemId` | UUID — must reference a StudyItem with the same `ownerUserId` |
| `weight` | decimal 0–5 |
| `rationale` | string |
| `createdAt` | timestamp |

EvidenceLinks have no proposal lifecycle. Existence means active; deletion removes their contribution to Demand. A link only ever connects a source and a StudyItem owned by the same user — there is no cross-user linking.

---

## StudyItemDetails

### LeetCodeDetails

| Field | Type |
|---|---|
| `problemNumber` | int > 0? |
| `url` | absolute https URL? |
| `difficulty` | Easy \| Medium \| Hard |
| `patterns` | normalised string[] |
| `expectedTimeComplexity` | string |
| `expectedSpaceComplexity` | string |
| `approachMarkdown` | string |
| `csharpSolution` | string? |

### SystemDesignDetails

| Field | Type |
|---|---|
| `promptMarkdown` | string |
| `clarifyingQuestions` | string[] |
| `functionalRequirements` | string[] |
| `nonFunctionalRequirements` | string[] |
| `evaluationChecklist` | string[] |
| `referenceSolutionMarkdown` | string |

Revealing the reference solution is transient UI state and is never persisted.

### BehavioralDetails

| Field | Type |
|---|---|
| `competencies` | string[] |
| `questionVariants` | string[] |
| `situation` | string |
| `task` | string |
| `action` | string |
| `result` | string |
| `reflection` | string? |

### TheoryDetails

| Field | Type |
|---|---|
| `summaryMarkdown` | string |
| `keyPoints` | string[] |
| `interviewQuestions` | string[] |
| `references` | absolute http/https URL[] |

---

## Value Objects

### PriorityOverride

```text
score:  int [0, 100]
reason: non-empty string
```

### JobSource

```text
PastedText:
  content: string

UploadedFile:
  storageObjectKey: backend-generated string
  originalFileName: string
  mimeType: application/pdf
  extractedText: string
```

### ContactInfo

```text
name: string
email: string
phone: string?
address: string?
photoStorageKey: string?
```

### YearMonth

```text
year: int
month: int [1, 12]
```

### ScoringWeights

```text
importanceWeight: int
demandWeight: int
masteryGapWeight: int
```

All weights are non-negative and sum to 100.

---

## Operational Persistence Models

These records support application/infrastructure concerns and are not domain aggregates.

### ScoringConfigOverride

At most one optional row per user (`ownerUserId`), containing ScoringWeights. Absence means the 40/35/25 code defaults apply for that user. The Application layer resolves override-or-defaults per user and supplies the resulting weights to that user's ranked queue query. There is no global/shared override — each user's weights are independent.

### AIUsageRecord

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `ownerUserId` | UUID | FK to the owning User (ADR-0015) — the user whose action triggered the AI call |
| `idempotencyKey` | string | Unique |
| `commandType` | enum | One of the three AI commands |
| `sourceType` / `sourceId` | enum / UUID | |
| `provider` / `model` | string | Metadata only |
| `pricingVersion` / `currency` | string / ISO 4217 code | Pricing snapshot used for estimates |
| `status` | Reserved \| Completed \| Failed | |
| `reservedInputTokens` / `reservedOutputTokens` | int | Pre-call limits |
| `reservedCost` | decimal | In `currency`; counts against budget while Reserved |
| `actualInputTokens` / `actualOutputTokens` | int? | Provider-reported |
| `actualCost` | decimal? | In `currency`; reconciled after completion |
| `analysisDraftId` | UUID? | Allows an idempotent replay to return the existing result |
| `startedAt` / `completedAt` | timestamp / timestamp? | UTC |
| `outcomeCode` | string? | Safe metadata; never provider content |

The budget reservation transaction checks Completed actual cost plus active Reserved cost for the daily and monthly windows before inserting the row. Completion reconciles the reservation; failure releases unused reservation while preserving the audit record.

---

## Domain Invariants

1. StudyItem status is Active or Archived; mastery never archives automatically.
2. StudyItem hard delete is allowed only with no StudyReviews and no EvidenceLinks.
3. Importance, InitialMastery, and StudyReview confidence are in `[1,5]`.
4. Tags are normalised and unique per StudyItem.
5. PriorityOverride score is `[0,100]` and its reason is non-empty.
6. StudyItem category and details discriminator must match.
7. EvidenceLink weight is `[0,5]`.
8. EvidenceLink is unique by `(sourceType, sourceId, targetStudyItemId)`.
9. EvidenceLinks are created only from accepted LinkProposals; there is no direct creation command.
10. AnalysisDraft transitions only `Pending → Applied` or `Pending → Discarded`.
11. At most one Pending AnalysisDraft exists per `(sourceType, sourceId)`.
12. Applying requires one final decision for every proposal, with no omissions or duplicates.
13. Original proposed payloads are immutable; accepted actionable proposals persist a separate complete accepted payload.
14. Proposal statuses become Accepted or Rejected only as part of Apply and are then immutable.
15. Applying a non-Pending draft is invalid.
16. A JobGap can reference only a requirement in the same JobAnalysis.
17. No JobGap exists for a fully matched requirement.
18. `otherLabel` is required when InterviewRound is Other.
19. Deleting a JobAnalysis sets optional InterviewNote references to null; it never deletes InterviewNotes.
20. ProfessionalProfile Skill.normalizedKey is unique.
21. Experience and Project skill IDs must exist in their owning profile.
22. A Skill referenced by Experience or Project entries cannot be deleted until those references are removed or reassigned.
23. CVPresentation selection IDs must exist in its referenced ProfessionalProfile.
24. Every CVPresentation selection collection has unique entry IDs and unique, contiguous positions starting at zero.
25. Deleting a canonical profile entry removes its CVPresentation selection rows but never deletes a CVPresentation.
26. ScoringWeights are non-negative integers summing to 100.
27. Computed EffectiveScore is `[8,100]` with default input ranges; an override may be `[0,100]`.
28. AIUsageRecord.idempotencyKey is unique.
29. Every cross-aggregate reference connects entities owned by the same user (see ADR-0015): a CVPresentation's `professionalProfileId`, an EvidenceLink's `sourceId`/`targetStudyItemId`, an AnalysisDraft's `sourceId`, and an InterviewNote's `jobAnalysisId` must all share the referencing entity's `ownerUserId`. There is no cross-user reference, ever.
30. ScoringConfigOverride and AIUsageRecord are scoped by `ownerUserId`; a user's budget and scoring weights never affect another user's.

---

## Cross-Aggregate References

All cross-aggregate references are IDs only; domain navigation properties never cross aggregate boundaries. Every reference below is additionally scoped to the same `ownerUserId` on both ends (invariant 29) — a user can only ever reference their own data.

| From | Field | References |
|---|---|---|
| CVPresentation | `professionalProfileId` | ProfessionalProfile |
| CVPresentation selections | `entryId` | Canonical child entries owned by its ProfessionalProfile |
| EvidenceLink | `targetStudyItemId` | StudyItem (FK, no cascade) |
| EvidenceLink | `sourceType + sourceId` | CVPresentation, JobAnalysis, or InterviewNote |
| AnalysisDraft | `sourceType + sourceId` | CVPresentation, JobAnalysis, or InterviewNote |
| InterviewNote | `jobAnalysisId?` | JobAnalysis |

The polymorphic references cannot have normal foreign keys and are validated by use cases. Source deletion, EvidenceLink cleanup, and AnalysisDraft cleanup follow ADR-0011.
