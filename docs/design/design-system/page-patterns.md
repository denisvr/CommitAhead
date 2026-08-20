# Page Patterns and Delivery Order

Visual references demonstrate composition only. Domain documents, API contracts and ADRs define the
behaviour.

## Delivery order

Do not generate all documented pages at once.

1. **Current foundation:** style the existing login/authenticated shell without changing the auth
   flow.
2. **Phase 2:** ProfessionalProfile and CVPresentation editing.
3. **Phase 5:** CV export.
4. **Next:** the Profile Studio composition described below, which restructures the Phase 2 pages
   without changing their contracts.

A later-phase visual may inform component extensibility, but its route and behaviour are not built
early.

## Login

A quiet, narrow form centred on the page ground: Bookmark lockup, one-sentence purpose, email field
and one primary action. Preserve the existing backend-mediated flow and generic response. The UI
never contains Supabase SDK logic or exposes whether an address is provisioned.

States: checking session, ready, submitting, generic link-sent confirmation, validation error,
expired/invalid callback and retryable server error.

## Application shell

A sticky, edge-to-edge header owns the brand (clickable, returns to Home), the theme control and
the account control (AccountMenu). Below it, a persistent left Sidebar owns primary navigation —
Home, Professional profile, CV presentations — modelled on Azure DevOps's own project sidebar,
collapsible to icon-only. The header carries no page-level action buttons; each feature page owns
its own actions in its own header row, the way Azure DevOps's board toolbar (not its global header)
carries "+ New Work Item".

Authentication state and logout behaviour remain owned by the existing app flow. Theme preference
changes tokens only; it must not duplicate component styling.

## Home

The landing view reached via the brand mark and the Sidebar's "Home" item. A shortcut hub, not a
dashboard: one card per feature (Professional profile, CV presentations) with a one-sentence
description and its own primary action. It carries no fabricated metrics — no completeness score,
no counts — since nothing in this slice aggregates that data yet; adding real numbers here is
future work, not something to mock convincingly.

## Profile Studio

The master profile is one scrolling page of stacked section cards, not a set of tabs. Tabs hide how
much of a career is recorded, which is the one thing this page exists to show.

**Composition.** Sticky header, then a sticky section jump bar with counters, then eight section
cards in a reading column capped at 820px, with a sticky 452px preview column beside it.

**Sections.** About you, Experience, Education, Skills, Languages, Certifications, Projects, Links.
Each is a card with an icon, a heading, a count or summary, an optional status badge and a lead
sentence explaining what belongs there.

**Entries.** Collapsible rows, collapsed by default except the one being worked on. A senior
profile is fifteen years of history; the page must be scannable without expanding anything.
Expanded, a row shows formatted read-only text first (Europass-style), not its input fields —
editing is an explicit action, not the default state of opening a row. Each card itself also
collapses to just its header, independent of its rows' own state.

**Profile is not a CV.** Copy throughout must reinforce that the profile stores everything and a CV
selects from it. Nothing in the profile suggests a target length, and the preview states plainly
when a template is printing fewer items than the profile holds.

**Coverage.** The coverage figure measures how much career knowledge is stored — it is not a CV
quality score, and it is not job match. Job match belongs to an individual CV and must never appear
on the profile page.

**Preview.** The profile rendered through a template, clearly labelled as a preview and not a saved
CV. It is bidirectional: selecting an element in the sheet opens the matching entry, and focusing an
entry highlights its line in the sheet.

## CV presentations

ProfessionalProfile is the canonical source. A CVPresentation selects and orders canonical entry
IDs; editing a presentation never duplicates or silently mutates canonical content. When a CV is
open, the preview shows that CV rather than the profile — the distinction must stay visible in the
interface, not only in the data.

## Destructive operations

Use literal confirmation copy describing the actual domain effect. Never claim a cascade or
replacement that the use case does not perform. When an invariant blocks deletion, present the
reason and the safe alternative instead of offering a destructive confirmation that must fail.

## Responsive baseline

Every implemented page must be reviewed at:

- wide desktop, preview column in the layout (1280px and above);
- everything below 1280px (tablet and phone alike), preview reached from the one "Preview" control
  in the page header, which opens it in a dialog;
- zoomed text without clipped controls.

Exactly one preview control is visible at any width — below 1280px that is always the header
button, not a second, narrower-still variant; a single simple control that works down to a phone
screen is preferred over two different mechanisms for two sub-ranges. Dense tables require a
deliberate mobile alternative (ProfessionalProfilePage's Skills table scrolls horizontally inside
its own bounded strip, the same technique as SectionNav's own jump bar, rather than being clipped
by its Card's `overflow: hidden`). Horizontal overflow is acceptable only for data whose comparison
would be destroyed by stacking, and must be keyboard accessible.

## Theme baseline

Every implemented page must also be reviewed in light, in dark, and with no explicit choice under a
dark system preference — the third is a distinct code path and is where missing tokens surface.
Native form controls, scrollbars and the focus ring are part of the review, not an afterthought.
