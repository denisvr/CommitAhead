# CommitAhead Design System

This directory is the canonical design reference for the CommitAhead frontend.

**Approved identity:** Reading Room

**Approved mark:** Bookmark

**Production approach:** React 19 + TypeScript, CSS Modules and shared CSS custom-property tokens
(ADR-0016)

The files here document the visual and content system. They are not a runtime package and must not
be imported directly by `frontend/`.

## Authority order

When implementing a page, use this order:

1. `CONTEXT.md`, `docs/domain/` and accepted ADRs define behaviour, invariants and terminology.
2. This file defines the visual, content and interaction language.
3. `components.md` defines reusable production component responsibilities.
4. `page-patterns.md` defines page composition and phase boundaries.
5. `identity/ReadingRoom.html` and `identity/Wordmark.html` are visual references only.

If a visual example conflicts with the domain, the domain wins. Never invent behaviour from a
mock.

## Product character

CommitAhead is a private, invite-only application for experienced software developers to maintain
their professional profile and produce tailored CV presentations from it.

Reading Room treats the application as a calm study desk rather than a DevOps console:

- warm paper and ink rather than blue-black dashboard chrome;
- one clear next action and a written reason;
- ranked rows and whitespace instead of floating cards;
- dense presentation only where analytical comparison requires it;
- colour reserved for meaning, never decoration.

The Bookmark mark represents continuity: the place the user has reached in their preparation.

## Required source files

```text
readme.md                    this contract
components.md                production component responsibilities
page-patterns.md             page composition and phase boundaries
styles.css                   reference token entry point
tokens/                      approved colours, type, space, elevation and motion
identity/ReadingRoom.html    visual foundation board
identity/Wordmark.html       approved Bookmark construction and usage
assets/fonts/                local Public Sans and IBM Plex Mono plus licences
assets/icons/                approved local Lucide source SVGs plus licence
assets/logo/bookmark/        approved lockups, symbols and favicons
```

When the first production slice needs them, copy the approved tokens and selected assets into
`frontend/src/design-system/`. The production copy becomes part of the application build; changes
to visual values must update both the implementation and this reference in the same PR.

## Colour

The exact values and semantic aliases are in `tokens/colors.css`.

- Light ground: paper `#F6F3EC`; content sheet `#FFFDF8`; ink `#1B1A17`.
- Dark ground: paper `#171614`; content sheet `#1E1D1A`; ink `#F0EDE6`.
- Accent: ink navy `#24405C` light and `#8FB4D9` dark.
- Semantic colours: brick (critical), ochre (caution), moss (good).

Accent is used for links, primary actions, active navigation and focus.
Semantic colours are always paired with text.

Do not introduce gradients, glass, blur, texture, decorative backgrounds or page-local colour
values. Every text/background combination must retain WCAG AA contrast.

## Typography

- Public Sans: all interface and prose.
- IBM Plex Mono: numeric columns and tracked uppercase micro-labels only.
- Minimum readable text: 12.5px.
- 11px is permitted only for uppercase, tracked, non-essential labels whose meaning appears
  elsewhere.
- Numeric columns use tabular figures.

Both families are bundled locally. Production must not use Google Fonts or any other font CDN.

## Layout and density

- Desktop sidebar: 200px.
- Under 768px: navigation becomes a mobile bottom bar or sheet appropriate to the flow.
- Main reading column: maximum 820px.
- Comfortable density is the default for the queue, forms, details and CV editing.
- Dense density is opt-in for analytical tables only; never apply it to a whole page or a mobile
  tap-target list.
- Mobile interactive targets are at least 44px.

Every page starts with a title and a concise statement of what is shown and, when relevant, how it
is ordered. Two-column detail layouts collapse to one column on narrow screens.

## Surfaces, borders and shape

- 0–4px radius only.
- Lists are rows separated by one soft rule.
- Groups are separated by whitespace.
- There is no generic Card component.
- Bounded regions use one border, no fill change and no shadow.
- Only dialogs, popovers and a mobile sheet may use the overlay shadow.
- Never place two adjacent separator rules.

## Motion and states

Motion is functional only: 80–180ms colour/background/border transitions. No entrances, bounce,
spring, shimmer or decorative animation. `prefers-reduced-motion` reduces durations to zero.

Hover changes colour or surface tint; it never lifts or transforms an element. Disabled controls
use reduced emphasis and remain semantically disabled. Destructive actions require confirmation.

Every interactive element has a visible 2px accent focus outline at 2px offset. Keyboard behaviour
must be complete, not merely focusable.

## Iconography and imagery

The approved Lucide source SVGs are bundled in `assets/icons/` at a 1.75 stroke. Production should
import the individual local SVGs through a typed icon component; it must not inject an SVG sprite
with `innerHTML`. `assets/icons/icons.js` exists only so the two static identity reference pages
can display their icon examples; it is not a production asset.

Icons are allowed in navigation and in familiar action controls where they improve recognition
(back, add, edit, download, theme, delete). They are not decoration: never place
them beside headings, as list bullets, inside body copy or in empty states without an action.
Buttons keep visible text unless the control is universally understood and has an accessible
name.

There is no decorative imagery. A CV photo is user-supplied, private and hidden by default unless
the CVPresentation explicitly includes it.

## Content rules

- Use the exact ubiquitous language from `CONTEXT.md`.
- In prose, render entity names as normal sentence-case words; reserve PascalCase for technical
  names.
- Sentence case everywhere.
- Address the user as “you”; the product does not say “I” or “we”.
- No gamification, praise, streaks or personality.
- No emoji.
- Explain the reason behind state changes.
- Errors are literal and blameless: what happened and what the user can do next.
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

See ADR-0016 for the implementation decision and `docs/security/threat-model.md` for the CSP and
rendering boundaries.

## Acceptance checklist for a frontend PR

- Uses the current roadmap slice only.
- Uses existing production tokens and primitives.
- Uses exact domain terminology and backend-provided computed values.
- Covers loading, empty, error, disabled and success states relevant to the flow.
- Works with keyboard and visible focus.
- Works at representative desktop and mobile widths.
- Introduces no inline style, external resource or CSP exception.
- Adds representative React Testing Library coverage.
- Does not weaken auth, CSRF or Markdown trust boundaries.
