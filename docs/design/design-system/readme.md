# CommitAhead Design System

This directory is the canonical design reference for the CommitAhead frontend.

**Approved identity:** Studio

**Approved mark:** Bookmark

**Production approach:** React 19 + TypeScript, CSS Modules and shared CSS custom-property tokens
(ADR-0016)

Studio replaced the previous Reading Room identity. See ADR-0024 for the decision and what was kept.

The files here document the visual and content system. They are not a runtime package and must not
be imported directly by `frontend/`.

## Authority order

When implementing a page, use this order:

1. `CONTEXT.md`, `docs/domain/` and accepted ADRs define behaviour, invariants and terminology.
2. This file defines the visual, content and interaction language.
3. `components.md` defines reusable production component responsibilities.
4. `page-patterns.md` defines page composition and phase boundaries.
5. `../prototypes/cv-3-studio-v2.1.html` is the approved reference layout — visual only.

If a visual example conflicts with the domain, the domain wins. Never invent behaviour from a mock.

## Product character

CommitAhead is a private, invite-only application for experienced software developers to maintain a
master professional profile and produce targeted CV presentations from it.

The profile is the source of truth; a CV is a derived, curated projection of it. The interface has
to make that separation obvious without a tutorial, which is what shapes the visual language:

- a workspace of discrete, editable objects — cards and collapsible rows, not a long form;
- the record is dense and complete; the derived document is short and selective, and both are
  visible at once;
- structure over decoration: a card is a boundary, not an ornament;
- one clear next action per screen, with the reason stated;
- colour reserved for severity and for the accent, never for category.

The Bookmark mark represents continuity: the place the user has reached in their preparation.

## Required source files

```text
readme.md                       this contract
components.md                   production component responsibilities
page-patterns.md                page composition and phase boundaries
styles.css                      reference token entry point
tokens/                         approved colours, type, space, elevation and motion
verify-contrast.mjs             checks every colour pair in both themes against tokens/colors.css
../prototypes/cv-3-studio-v2.1.html   approved reference layout, both themes
identity/Wordmark.html          approved Bookmark construction and usage — still current
identity/ReadingRoom.html       historical: the superseded identity (ADR-0024)
assets/fonts/                   local Public Sans and IBM Plex Mono plus licences
assets/icons/                   approved local Lucide source SVGs plus licence
assets/logo/bookmark/           approved lockups, symbols and favicons
```

`frontend/src/design-system/tokens/` is a copy of `tokens/` here, carrying a header that says so.
This directory is the source of truth: change a value here and copy it across in the same PR. Never
edit only one side, and never let production mint a token this reference does not describe.

## Colour

The exact values and semantic aliases are in `tokens/colors.css`.

- Light: page `#EEF1F6`, card `#FFFFFF`, ink `#101828`.
- Dark: page `#0E121A`, card `#171D28`, ink `#E8EDF6`.
- Accent: indigo `#4F46E5` light, `#A5AAFB` dark.
- Semantic colours: emerald (good), amber (recommendation), rose (critical).

Accent is used for links, primary actions, active navigation, the current jump-bar section and
focus. Semantic colours are always paired with text — a badge never communicates by colour alone.

### The two themes are one system

Light and dark are not independent palettes. They express the same relationship:

> the page ground is one step away from the card, and the card is where content lives.

Light puts a white card on a grey page. Dark puts a lighter slate card on a near-black page. That
symmetry is why the dark theme reads as crisply as the light one, and why a card is always
recognisable as a card.

Depth is the one thing the two themes express differently, because they have to. In light, a card
separates from the page mainly through `--shadow-sm`. On a dark ground a shadow carries almost
nothing, so the card edge does that work instead — `--card-border` resolves to `--border-soft` in
light and to the stronger `--border` in dark. Read that token as "whatever currently delimits a
card", never as a fixed colour, and never hard-code either value.

Dark is authored twice: once under `:root[data-theme="dark"]` for an explicit choice, once under
`@media (prefers-color-scheme: dark)` guarded by `:root:not([data-theme="light"])` for the system
default. Any new token must be added to both blocks. `verify-contrast.mjs` fails if they drift.

Set `color-scheme` per theme so native controls — selects, checkboxes, date inputs, scrollbars —
follow. Skipping this is the most common way a dark theme ends up with bright form controls.

### Contrast is measured, not judged

Run it:

```bash
node docs/design/design-system/verify-contrast.mjs
```

It reads `tokens/colors.css`, resolves the semantic aliases and checks every pair the product
actually renders, in both themes. Body text is held to 4.5:1; control edges and focus rings to
3:1 (WCAG 1.4.11); structural separation to our own legibility floor. It exits non-zero on failure,
so the ratios quoted here can never drift from the tokens.

The narrowest passing margins, worth knowing before you change a value:

| Pair | Light | Dark |
|---|---|---|
| metadata on the page ground | 4.95 | 6.18 |
| success badge | 4.75 | 8.12 |
| input border on a sunken region | 3.16 | 3.57 |
| card lift away from the page | 1.13 | 1.11 |

Do not introduce gradients on content surfaces, glass, blur as decoration, texture or page-local
colour values. The mark and the primary-button glow are the only gradients or coloured shadows in
the system.

## Typography

- Public Sans: all interface and prose.
- IBM Plex Mono: figures the user compares or scans — counts, percentages, years, date ranges, CEFR
  levels, ordinal row positions — and tracked uppercase micro-labels. Never prose, labels or entity
  names.
- Minimum readable text: 12px.
- 11.5px is permitted only for uppercase tracked labels whose meaning appears elsewhere.
- Numeric columns and any figure that updates in place use tabular figures.

Both families are bundled locally. Production must not use Google Fonts or any other font CDN.

## Layout and density

- Page header is sticky and holds the profile/CVs switch, the theme control and page actions.
- The section jump bar sticks directly beneath the header and is navigation, not tabs: links with a
  scroll-spy `aria-current`, keyboard-reachable in document order.
- Reading column: maximum 820px (`--content-max`). The workspace page may be wider (up to 1360px, `--page-max`) because the profile
  preview occupies a second column — these are two different limits, do not conflate them.
- The preview column is 452px and sticky. Below 1280px it leaves the layout and is reached through
  one control; below 720px that control is the floating button. Never show two ways to open it at
  once.
- Comfortable density is the default. Dense is opt-in for analytical tables only — currently the
  skills metadata table. Never apply it to a page or a mobile tap-target list.
- Mobile interactive targets are at least 44px.

Every page starts with a title and a concise statement of what is shown and, when relevant, how it
is ordered. Two-column layouts collapse to one column on narrow screens.

## Surfaces, borders and shape

- A card is a real, named boundary: `--surface`, one `--card-border` hairline, `--radius-xl`, and
  `--shadow-sm` in light. Cards are separated from one another by `--card-gap` whitespace, never by a rule;
  `--section-gap` is the wider rhythm between top-level page regions.
- Inside a card, lists are collapsible rows separated by one soft hairline. An open row is marked by
  an accent left edge and its own border, not by a fill change.
- Sunken regions (`--surface-sunken`) are for content nested inside a card that is not itself a
  card — the achievements repository, an inline AI panel.
- Radii are assigned by role in `tokens/space.css`, from 4px to 14px, and a child's radius is always
  smaller than its parent's. Do not pick a radius by eye.
- Shadow belongs to cards, the preview sheet, the floating button and overlays. Nothing else. A
  shadow never signals state.
- Never place two adjacent separator rules.

## Motion and states

Motion is functional only: 80–180ms colour, background and border transitions, 180ms for a
disclosure, 240ms for the single attention flash that confirms a jump landed. No entrances, bounce,
spring, shimmer or decorative animation. `prefers-reduced-motion` reduces durations to zero.

Hover changes colour or surface; it never lifts, scales or moves an element. Transform is permitted
only for a disclosure chevron. Disabled controls use reduced emphasis and stay semantically
disabled. Destructive actions require confirmation.

Every interactive element has a visible 2px accent focus outline at 2px offset. A collapsible row
header is a `<button>` carrying `aria-expanded`. Keyboard behaviour must be complete, not merely
focusable.

## Iconography and imagery

The approved Lucide source SVGs are bundled in `assets/icons/` at 1.75 stroke. Production imports
the individual local SVGs through a typed icon component; it must not inject an SVG sprite with
`innerHTML`.

Studio uses icons more than Reading Room did, and deliberately: each profile section carries an icon
in a tinted square beside its heading, because eight stacked sections on one page need a scannable
anchor. That is the one decorative-adjacent use the system allows, and it is allowed only there —
one icon per section heading, drawn from the approved set, never invented.

Otherwise icons remain functional: navigation, the theme control, and familiar action controls
(back, add, edit, delete, download, close, preview, expand). They are never used as list bullets,
inside body copy, or in an empty state without an action. Buttons keep visible text unless the
control is universally understood and has an accessible name.

There is no decorative imagery. A CV photo is user-supplied, private and hidden by default unless
the CVPresentation explicitly includes it.

## The preview sheet is not chrome

The profile preview renders a document. It uses the `--paper-*` tokens, which are deliberately
theme-independent and stay light in both themes — the same reason the exported PDF is light: it
shows what a recruiter will actually read. Nothing outside the preview may use those tokens, and the
preview may not use the theme tokens for its sheet.

## Content rules

- Use the exact ubiquitous language from `CONTEXT.md`.
- In prose, render entity names as normal sentence-case words; reserve PascalCase for technical
  names.
- Sentence case everywhere.
- Address the user as "you"; the product does not say "I" or "we".
- No gamification, praise, streaks or personality.
- No emoji.
- Explain the reason behind state changes.
- Errors are literal and blameless: what happened and what the user can do next.
- Distinguish the three severities in words as well as colour: a data error states what is wrong, a
  recommendation states what would improve, an optional section is simply labelled optional and is
  never framed as incomplete.
- Do not invent identifiers, telemetry or domain values to decorate a layout.

## Production restrictions

- No inline `style` attributes or CSS-in-JS.
- No CDN fonts, scripts, icons or images.
- No runtime-generated style tags or injected SVG/HTML markup.
- No `window` globals.
- No direct Supabase calls.
- No imports from `docs/design/`.
- No copying reference HTML or prototype code into production.
- No page-local substitute for an existing production design-system primitive.

See ADR-0016 for the implementation decision, ADR-0024 for the identity decision, and
`docs/security/threat-model.md` for the CSP and rendering boundaries.

## Acceptance checklist for a frontend PR

- Uses the current roadmap slice only.
- Uses existing production tokens and primitives.
- Renders correctly in light, in dark, and with the system preference and no explicit choice.
- Any new colour token exists in both dark blocks, and `verify-contrast.mjs` passes.
- Uses exact domain terminology and backend-provided computed values.
- Covers loading, empty, error, disabled and success states relevant to the flow.
- Works with keyboard and visible focus; disclosures carry `aria-expanded`.
- Works at representative desktop, tablet and mobile widths, with exactly one preview control at
  each.
- Introduces no inline style, external resource or CSP exception.
- Adds representative React Testing Library coverage.
- Does not weaken auth, CSRF or Markdown trust boundaries.
