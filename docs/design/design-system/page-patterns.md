# Page Patterns and Delivery Order

Visual references demonstrate composition only. Domain documents, API contracts and ADRs define
the behaviour.

## Delivery order

Do not generate all documented pages at once.

1. **Current foundation:** style the existing login/authenticated shell without changing the auth
   flow.
2. **Phase 1:** Study queue, StudyItem list/detail, typed forms, review flow, tag input and scoring
   settings.
3. **Phase 2:** ProfessionalProfile and CVPresentation editing.
4. **Phase 3:** JobAnalysis and InterviewNote management.
5. **Phase 4:** AI analysis and AnalysisDraft review.
6. **Phase 5:** CV export.

A later-phase visual may inform component extensibility, but its route and behaviour are not built
early.

## Login

A quiet, narrow form on the paper ground: Bookmark lockup, one-sentence purpose, email field and
one primary action. Preserve the existing backend-mediated flow and generic response. The UI never
contains Supabase SDK logic or exposes whether an address is provisioned.

States: checking session, ready, submitting, generic link-sent confirmation, validation error,
expired/invalid callback and retryable server error.

## Application shell

Desktop uses the 200px sidebar and one 820px reading column. Mobile replaces the sidebar below
768px; it does not merely squeeze it. Authentication state and logout behaviour remain owned by
the existing app flow.

The active destination uses surface tint plus navy icon/text emphasis. Theme preference changes
tokens only; it must not duplicate component styling.

## Study queue

Lead with one StudyItem: what to study next, its EffectiveScore and a written explanation. Follow
with the remaining ordered rows. Category filters are secondary and monochrome.

All computed values and reasons come from the ranked-queue API projection. Do not derive Mastery,
Demand, EffectiveScore or sort order in React.

States: loading, no active StudyItems, populated queue, API error and archived-item exclusion.

## StudyItem detail and review

Header contains title, category and valid actions. Details render the typed category variant.
Review history and EvidenceLinks are separate meaningful regions.

The review form captures confidence 1–5 and optional notes. A successful save refreshes the
server-provided Mastery and EffectiveScore.

Hard delete is available only when there are no StudyReviews and no EvidenceLinks. Otherwise the
UI explains the guard and offers Archive. Archival is always explicit and never mastery-driven.

SystemDesign reference solutions are hidden with transient component state until the user reveals
them.

## Professional profile and CV presentations

ProfessionalProfile is the canonical source. A CVPresentation selects and orders canonical entry
IDs; editing a presentation never duplicates or silently mutates canonical content.

Use comfortable density for editing. Preview is bounded by a rule, not a floating card. On narrow
screens, editor and preview become tabs/stacked regions rather than scaled-down side-by-side
panes.

## Job analysis and interview notes

JobAnalysis clearly identifies PastedText or UploadedFile provenance. Requirements and JobGaps may
use the dense analytical table pattern. Upload UI shows constraints and literal parsing failures.

InterviewNote remains a structured record and may be linked to a JobAnalysis; it is not a freeform
diary page.

## AI analysis draft

The analysis trigger belongs to its evidence source and displays budget/cost context. If that
source already has a Pending AnalysisDraft, another analysis is blocked; the existing draft is
never silently replaced.

The review screen presents every immutable proposal and requires one Accepted/Rejected decision
per proposal. Accepted actionable proposals expose user-finalised fields, including
InitialMastery for a StudyItemProposal. Apply submits the full decision set atomically. Discard is
explicit.

## Destructive operations

Use literal confirmation copy describing the actual domain effect. Never claim a cascade or
replacement that the use case does not perform. When an invariant blocks deletion, present the
reason and the safe alternative instead of offering a destructive confirmation that must fail.

## Responsive baseline

Every implemented page must be reviewed at:

- wide desktop with sidebar;
- narrow desktop/tablet;
- mobile navigation under 768px;
- zoomed text without clipped controls.

Dense tables require a deliberate mobile alternative. Horizontal overflow is acceptable only for
data whose comparison would be destroyed by stacking, and must be keyboard accessible.
