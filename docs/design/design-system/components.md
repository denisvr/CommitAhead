# Production Component Contract

This is a responsibility inventory, not a demand to implement every component up front. Add a
component only when the current roadmap slice needs it. Production components are TypeScript plus
CSS Modules and consume shared tokens.

## Core

### Brand

Renders the approved Bookmark symbol and CommitAhead wordmark. Use the supplied outlined SVG
lockups where appropriate; use the standalone symbol at constrained sizes. The brand is not a
click target unless it has an actual navigation destination.

### Button and IconButton

Variants: primary, secondary, ghost and danger. A screen has one visually primary action.
Icon-only buttons require an accessible name. Loading preserves control width and prevents repeat
submission. Danger actions open a confirmation dialog.

### Field, Input, Textarea, Select and Checkbox

Labels are explicit and programmatically associated. Errors are connected with
`aria-describedby`; hints remain available when an error appears. Native controls are preferred.
Do not use placeholder text as a label.

### Dialog

Uses an accessible dialog implementation with initial focus, focus containment, Escape handling,
focus return and a labelled title. Clicking the scrim may cancel only when cancellation is safe.

### Tabs

Implements the ARIA tabs pattern: arrow-key navigation, selected state, tab/tabpanel association
and predictable focus. On mobile, tabs may scroll horizontally without hiding the active tab.

### Callout

Communicates information, caution or error with text first. Tone never depends on colour alone.
Use `role="alert"` only for new errors requiring immediate announcement.

### Chip and Badge

Chip is a compact filter/tag control or static category label. Badge communicates a small status.
Neither is a generic container. Categories remain monochrome.

### EmptyState

Explains why the region is empty and offers one relevant next action when available. It contains
no decorative illustration, celebration or generic filler.

## Navigation

### AppShell

Owns the desktop sidebar, responsive mobile navigation, content surface and theme control. Primary
destinations:

1. Study queue
2. Study items
3. Professional profile & CVs
4. Job analyses
5. Interview notes
6. Settings

StudyItem categories are filters, never navigation destinations. AI is contextual, never a
destination.

### PageHeader

Contains page title, concise explanatory summary and optional actions. It does not duplicate
breadcrumbs or add decorative icons.

## Domain components

### RatingScale

The 1–5 control for Importance, InitialMastery and StudyReview confidence. It uses a radiogroup
model, supports arrow keys, exposes the selected value and meets the 44px mobile target.

### ScoreNumeral and ScoreBreakdown

Display the API-provided EffectiveScore and its Importance, Demand and Mastery-gap contributions.
They explain ranking; they do not calculate it. The visualisation has an equivalent accessible
text description.

### QueueRow

Represents one StudyItem in the ranked queue. Use a semantic link when the row navigates. It shows
title, category, EffectiveScore and a concise API-backed reason. Rows are not cards.

### ProposalDecision

Shows one immutable AI proposal, rationale and transient Accepted/Rejected choice. Accepted
actionable proposals expose their complete editable final payload. Apply remains unavailable until
every proposal has exactly one decision.

### AnalyticalTable

Used only for truly comparative data such as job requirements or AI usage. It uses the dense
region scale on desktop, preserves real table semantics and changes to an accessible stacked
presentation when columns cannot fit on mobile.

## Implementation rules

- Prefer native elements and links over `div role="button"`.
- Never add keyboard behaviour as an afterthought; define it with the component.
- Keep state and API orchestration in feature components/use-case hooks, not visual primitives.
- Keep domain terms in typed props; avoid generic bags of display data when the structure matters.
- Dynamic chart geometry uses SVG attributes or semantic classes, not inline styles.
- Do not create a generic Card, Toast, Avatar, Accordion, Breadcrumb or Pagination until a real
  current-slice use case requires one.
