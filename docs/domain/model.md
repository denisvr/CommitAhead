# CommitAhead — Domain Model

For term definitions see `CONTEXT.md`. For decisions see `docs/adr/`.

---

## Aggregates

### 1. StudyItem *(aggregate root)*

The primary unit of preparation. Lives in the ranked study queue.

**Persisted fields:**

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `title` | string | Canonical name; not duplicated in details |
| `category` | enum | Theory \| LeetCode \| SystemDesign \| Behavioral |
| `status` | enum | Active \| Archived |
| `importance` | int 1–5 | Manual; never computed |
| `initialMastery` | int 1–5 | Self-assessed; used until first StudyReview |
| `tags` | string[] | Normalised: trimmed, lowercase, kebab-case, unique per item |
| `details` | typed union | See category details below |
| `priorityOverride` | PriorityOverride? | Null = use computed score |
| `createdAt` | timestamp | |
| `updatedAt` | timestamp | |

**Computed at query time (never persisted):**

| Computed field | Formula |
|---|---|
| `mastery` | `initialMastery` before first review; average of up to 3 most recent `StudyReview.confidenceRating` values |
| `demand` | `min(Σ confirmed EvidenceLink.weight for links targeting this item, 5)` |
| `effectiveScore` | `(importance/5)×40 + (demand/5)×35 + ((5−mastery)/4)×25` OR `priorityOverride.score` when set |

**Children (inside aggregate boundary):**
- `StudyReview[]`

---

### 2. ProfessionalProfile *(aggregate root — singleton)*

The single canonical record of professional identity. One instance per installation.

**Persisted fields:** `id`, `contactInfo` (ContactInfo value object), `summary` (Markdown), `createdAt`, `updatedAt`

**Canonical collections (children, selected and ordered by CVPresentations):**
- `ExperienceEntry[]`
- `EducationEntry[]`
- `Skill[]`
- `LanguageEntry[]`
- `CertificationEntry[]`
- `ProjectEntry[]`
- `ProfileLink[]`

**Children (also inside aggregate):**
- `CVPresentation[]` — each references canonical entries by ID; entries are never duplicated

---

### 3. JobAnalysis *(aggregate root)*

A record created for a specific job posting. Holds the raw source and the extracted structure.

**Persisted fields:**

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `title` | string | User-assigned label |
| `jobSource` | JobSource | Discriminated union (see Value Objects) |
| `requirements` | JobRequirement[] | Extracted or manually added |
| `gaps` | JobGap[] | One per unmatched/partially-matched requirement |
| `notes` | string? | Free-form Markdown |
| `createdAt` | timestamp | |
| `updatedAt` | timestamp | |

---

### 4. InterviewNote *(aggregate root)*

A structured record of one real interview.

**Persisted fields:**

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `company` | string | |
| `role` | string | |
| `interviewRound` | InterviewRound enum | |
| `sequenceNumber` | int | Position in the interview process |
| `otherLabel` | string? | Required when `interviewRound = Other` |
| `date` | date | |
| `questions` | string[] | Questions asked |
| `gaps` | string[] | Weaknesses observed |
| `lessons` | string[] | Takeaways |
| `jobAnalysisId` | UUID? | Optional link to a JobAnalysis |
| `createdAt` | timestamp | |

---

### 5. AnalysisDraft *(aggregate root)*

The output of one AI analysis command. Holds typed proposals pending human review.

**Persisted fields:**

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `sourceType` | enum | CVPresentation \| JobAnalysis \| InterviewNote |
| `sourceId` | UUID | |
| `status` | enum | Pending \| Applied \| Discarded |
| `createdAt` | timestamp | |
| `appliedAt` | timestamp? | |

**Children (inside aggregate):**
- `SuggestionProposal[]` — either StructuredSuggestion or AdvisorySuggestion
- `LinkProposal[]` — proposed EvidenceLinks
- `StudyItemProposal[]` — proposed new StudyItems

Each proposal carries: `id`, `status (Pending | Accepted | Rejected)`, and type-specific payload.

---

### 6. EvidenceLink *(aggregate root)*

A confirmed, explicit connection from an evidence source to a StudyItem.

**Persisted fields:**

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `sourceType` | enum | CVPresentation \| JobAnalysis \| InterviewNote |
| `sourceId` | UUID | Polymorphic reference |
| `targetStudyItemId` | UUID | FK → StudyItem (no cascade) |
| `weight` | decimal 0–5 | Confirmed weight |
| `rationale` | string | Why this link was proposed |
| `createdAt` | timestamp | |

---

## Value Objects

### PriorityOverride
```
score:  int [0, 100]
reason: string (non-empty)
```
Stored inline on StudyItem. Presence implies override is active.

### JobSource *(discriminated union)*
```
PastedText:
  content: string

UploadedFile:
  storageObjectKey: string   ← backend-generated quarantine key; never the original filename
  originalFileName: string
  mimeType: string
  extractedText: string      ← extracted once at upload; AI always receives this field
```

### JobRequirement
```
id:            UUID
text:          string
kind:          Technical | Behavioural | Experience | Domain | Language | Other
priority:      Required | Preferred
sourceExcerpt: string
```

### JobGap
```
id:            UUID
requirementId: UUID   ← references parent JobRequirement
matchLevel:    Partial | Missing | Unknown
severity:      High | Medium | Low
rationale:     string
```
Only created when `matchLevel ∈ {Partial, Missing, Unknown}`. No gap is created for a fully matched requirement.

### ContactInfo
```
name:    string
email:   string
phone:   string?
address: string?
photoStorageKey: string?   ← Supabase Storage key; never a public URL in the domain
```

### YearMonth
```
year:  int
month: int [1, 12]
```
No day precision. Used in ExperienceEntry, EducationEntry, CertificationEntry, ProjectEntry.

---

## Domain Invariants

### StudyItem
1. `status ∈ {Active, Archived}`
2. Hard delete permitted only when `StudyReview[]` is empty **and** no `EvidenceLink` targets this item. Otherwise must be archived.
3. `importance ∈ [1, 5]`
4. `initialMastery ∈ [1, 5]`
5. Tags: each tag is trimmed, lowercase, kebab-case; no duplicates per item.
6. Mastery is never auto-archived based on its value; archival is always explicit.

### StudyReview
7. `confidenceRating ∈ [1, 5]`

### PriorityOverride
8. `score ∈ [0, 100]`
9. `reason` must be non-empty.

### EvidenceLink
10. `weight ∈ [0, 5]`
11. At most one `EvidenceLink` per `(sourceType, sourceId, targetStudyItemId)` — enforced by a unique database constraint.
12. Created only from an accepted `LinkProposal`; no direct creation path exists.

### AnalysisDraft
13. Status transitions: `Pending → Applied` or `Pending → Discarded` only. No reversal.
14. At most one `Pending` draft per `(sourceType, sourceId)` — enforced by a partial unique database index.
15. Proposal status transitions: `Pending → Accepted` or `Pending → Rejected` only. No reversal.
16. Applying a non-Pending draft is invalid.

### JobGap
17. No `JobGap` may exist for a `JobRequirement` that is fully matched.

### InterviewNote
18. `otherLabel` is required when `interviewRound = Other`.

### ScoringConfig
19. All three weights must be non-negative integers summing exactly to 100.

### EffectiveScore
20. Computed score range: `[8, 100]` (minimum when `importance=1, demand=0, mastery=5`).
21. `PriorityOverride.score` range: `[0, 100]`.

### CVPresentation
22. All IDs in `selectedExperienceIds`, `selectedEducationIds`, `selectedSkillIds`, `selectedLanguageIds`, `selectedCertificationIds`, `selectedProjectIds`, and `selectedProfileLinkIds` must reference existing entries in the owning `ProfessionalProfile`. Validated in the use case and enforced by FK constraints.

---

## Configuration (not an aggregate)

### ScoringConfig
```
importanceWeight: int   default 40
demandWeight:     int   default 35
masteryGapWeight: int   default 25
```
Defaults held in code. A single optional database row persists user overrides. The scoring domain service resolves override-or-defaults at query time.

---

## Cross-Aggregate References

All cross-aggregate references are by UUID only — no navigation properties cross aggregate boundaries.

| From | Field | References |
|---|---|---|
| `EvidenceLink` | `targetStudyItemId` | `StudyItem.id` (FK, no cascade) |
| `EvidenceLink` | `sourceId` + `sourceType` | Polymorphic: `CVPresentation`, `JobAnalysis`, or `InterviewNote` |
| `AnalysisDraft` | `sourceId` + `sourceType` | Polymorphic: same as above |
| `InterviewNote` | `jobAnalysisId?` | `JobAnalysis.id` (optional, informational) |
| `JobGap` | `requirementId` | `JobRequirement.id` (within `JobAnalysis` aggregate) |
| `ExperienceEntry` | `skillIds[]` | `Skill.id` (within `ProfessionalProfile` aggregate) |
| `ProjectEntry` | `skillIds[]` | `Skill.id` (within `ProfessionalProfile` aggregate) |
| `CVPresentation` | `selected*Ids[]` | Canonical collection entries (within `ProfessionalProfile`) |
