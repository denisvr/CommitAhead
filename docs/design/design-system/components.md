# Production Component Contract

This is a responsibility inventory, not a demand to implement every component up front. Add a
component only when the current roadmap slice needs it. Production components are TypeScript plus
CSS Modules and consume shared tokens.

## Core

### Brand

Renders the approved Bookmark symbol and CommitAhead wordmark. Use the supplied outlined SVG lockups
where appropriate; use the standalone symbol at constrained sizes. In Studio the symbol is tinted
with `--accent`. The brand is not a click target unless it has an actual navigation destination.

### Button and IconButton

Variants: primary, secondary, ghost, danger, success and accent. A screen has one visually primary
action. The primary variant carries `--accent-glow`; no other control has a coloured shadow.
Icon-only buttons require an accessible name. Loading preserves control width and prevents repeat
submission.

Success (green) and accent (the brand indigo) exist for the recurring Add/Edit pair on a
ProfessionalProfile list section — "+ Add …" and "Edit" always read the same colour everywhere
they appear, and "Done" (which both exits edit mode and persists, since these collections have no
per-entry Save) reuses success too. Danger (red) is Delete's colour throughout. A danger action
that is hard to reverse (deleting a whole CVPresentation) opens a confirmation dialog; removing one
entry from a list the user is actively editing (a skill, a certification, an achievement) does not
— undoing it is just adding the entry back, so the extra step would be friction without a
proportionate safety benefit.

### Field, Input, Textarea, Select and Checkbox

Labels are explicit and programmatically associated. Errors are connected with `aria-describedby`;
hints remain available when an error appears. Native controls are preferred — with `color-scheme`
set per theme they follow light and dark correctly. Control borders use `--border-strong`, which is
the only border token that clears 3:1. Do not use placeholder text as a label.

### Card

The primary structural surface: `--surface`, one `--card-border` hairline, `--radius-xl`, and
`--shadow-sm`. A card holds one profile section — a heading, an optional icon, a summary line, an
optional status badge, and its content. Cards are separated by `--card-gap` whitespace, never by a rule,
and are never nested inside one another. A card is a boundary, not a decoration: if a region has no heading
and no independent status, it is not a card.

The whole card collapses to just its header via a chevron toggle at the end of the head row —
separate from `actions`/`badge` so it never nests a button inside one. Defaults open; not
persisted, so a full page reload resets every card to expanded.

### CollapsibleRow

A repeated entry inside a card — one position, one qualification, one certification. Collapsed it
shows title, organisation, a metadata line and at most one status. Expanded, it defaults to a
read-only formatted view of the entry (Europass-style: text first, not input fields) with Edit and
Delete actions; Edit switches that same row into its field set, with Delete and Done to leave edit
mode. The header is a `<button>` carrying `aria-expanded`; the open state is marked by an accent
left edge, never by a fill change. Rows are separated by one soft hairline. Ordering is
chronological by default with manual reordering opt-in via Move up/down controls in the row body,
plus a leading drag handle for native HTML5 drag-and-drop (mouse/trackpad only — the Drag and Drop
API has no keyboard path, which is why the buttons remain the primary, always-present mechanism).
Not a JS drag library: this app's CSP (`style-src 'self'`, no `unsafe-inline`) blocks the inline
transform style every such library uses for its live drag feedback; the browser's own native drag
ghost image isn't part of the page's style pipeline, so it's unaffected.

### SectionNav

The sticky jump bar. Links with counters and a scroll-spy `aria-current`, not tabs: it navigates
within one page rather than swapping panels, so it must not implement the ARIA tabs pattern. It
scrolls horizontally on narrow screens without hiding the active item.

### Dialog

Uses an accessible dialog implementation with initial focus, focus containment, Escape handling,
focus return and a labelled title. Backdrop uses `--scrim`, surface uses `--shadow-overlay`.
Clicking the scrim may cancel only when cancellation is safe.

### Callout

Communicates information, caution or error with text first. Tone never depends on colour alone. Use
`role="alert"` only for new errors requiring immediate announcement.

### Chip and Badge

Chip is a compact tag or a small add control. Badge communicates status at one of three severities,
and the severity is carried by the words as much as by the colour:

- **critical** — a real data defect: wrong content pasted, an impossible date, a broken URL.
- **caution** — a recommendation that would improve the profile.
- **neutral** — informational, including anything genuinely optional. An optional section is never
  rendered as a problem.

Neither is a generic container. Categories remain monochrome.

### ThemeToggle

Three states — light, dark, system — because the token layer is authored for exactly those three.
Selecting a theme sets `data-theme`; selecting system removes the attribute so
`prefers-color-scheme` applies. Buttons carry `aria-pressed`. It lives in the application header.

### EmptyState

Explains why the region is empty and offers one relevant next action when available. It contains no
decorative illustration, celebration or generic filler.

## Navigation

### AppShell

Owns the sticky header (brand, theme control, account control), the Sidebar navigation rail, and
the content surface. The header is edge-to-edge, not centered on `--page-max`, so its brand mark
lines up with the sidebar's own left edge below it; only the content surface caps at `--page-max`
— a page decides its own reading column. The header carries no page-level action buttons — those
belong to the feature page itself (e.g. "Import from LinkedIn" on the profile page, "New CV
presentation" on the CV presentations list), the same way Azure DevOps's own global header stays
generic while a board's own toolbar carries its "+ New Work Item" action.

Clicking the brand mark returns to the hub's Home — the same destination as the Sidebar's own
"Home" item, offered as a second, familiar entry point rather than the only one.

### Sidebar

A persistent left navigation rail, collapsible to an icon-only rail and back (preference persisted
in `localStorage`, `design-system/sidebar.ts`). Modelled directly on Azure DevOps's own project
sidebar at the user's request — this is deliberately not derived from cv-3-studio-v2.1.html, which
has no such rail. Forced to icon-only below 767px rather than hidden, so its destinations stay
reachable on a phone-width screen. Items carry full feature names, not abbreviations.

Current items:

1. Home (landing page)
2. Professional profile
3. CV presentations

### AccountMenu

A circular avatar (initials derived from the signed-in email — `/api/me` has no display name) that
expands a small panel on click, copying Azure DevOps's own avatar → panel interaction. The panel
shows only what CommitAhead actually has: the email and a Log out action. It never shows a
"switch directory" control or a multi-account list — there is exactly one real user and no such
feature (CLAUDE.md).

### PageHeader

Contains page title, concise explanatory summary and optional actions. It does not duplicate
breadcrumbs or add decorative icons.

## Profile-specific

### AchievementRepository

The impact list inside an experience row. Unbounded by intent — the profile stores everything and a
CV selects from it — with a live count, drag reordering, and a growing final input. Its copy must
never imply a target number.

### ProfilePreview

Renders the profile through a template as a document. Uses the `--paper-*` tokens and stays light in
both themes. Selecting an element scrolls to and opens the matching profile entry; focusing or
hovering a profile field highlights the corresponding line in the sheet. Below 1280px it moves into
a dialog reached by exactly one control.

## Implementation rules

- Prefer native elements and links over `div role="button"`.
- Never add keyboard behaviour as an afterthought; define it with the component.
- Keep state and API orchestration in feature components/use-case hooks, not visual primitives.
- Keep domain terms in typed props; avoid generic bags of display data when the structure matters.
- Dynamic chart geometry uses SVG attributes or semantic classes, not inline styles.
- Every component must be checked in light, in dark, and with no explicit theme set.
- Do not create a generic Toast, Avatar, Breadcrumb or Pagination until a real current-slice use
  case requires one.
