# CommitAhead Design System

CommitAhead is a **private, invite-only web application for experienced software developers preparing for technical interviews**. It combines study topics, professional profile and CV data, job postings, and real interview evidence into a transparent ranked queue that answers one question: *what should I study next, and why?*

This project is the brand and product design system for that application. It is **not** the production React app — the repository's `frontend/` is deliberately unstyled at time of writing.

> **Status.** The visual direction is decided: **Reading Room**. `styles.css` ships it. Three wordmark proposals are out for approval in `identity/ReadingRoom.html` — the component library and UI kit follow once one is picked.

---

## Sources

| Source | What was taken from it |
|---|---|
| <https://github.com/denisvr/CommitAhead> (`main`) | Everything factual in this system |
| `docs/design/visual-identity.md` | Direction A's rationale and palette (explored, not chosen) |
| `docs/design/mockup.html` | Direction A's exact values and its five reference screens |
| `CONTEXT.md` | Ubiquitous language and the *avoid* vocabulary list — the basis of the content rules below |
| `docs/product/brief.md` | Product purpose, the five design principles, MVP scope |
| `docs/domain/model.md` | The EffectiveScore formula, entity fields, enum values used in every mock |
| <https://github.com/google/fonts> | Public Sans and IBM Plex Mono binaries (OFL) |
| <https://github.com/lucide-icons/lucide> | 25 icon SVGs (ISC) |

Nothing here assumes the reader has repository access. **Read the CommitAhead repository directly** — particularly `CONTEXT.md` and `docs/domain/model.md` — before designing anything new for this product; the domain vocabulary is unusually precise and getting it wrong is immediately visible.

There is **no logo** in the source repository, no icon set, no font binaries, and no brand imagery. Nothing has been reconstructed from memory. The three wordmarks in `assets/logo/` are new proposals for approval, drawn from product concepts (a rule, a bookmark, a ranked queue) and set in the bundled typeface.

---

## Product surfaces

One product, one surface: a **desktop-and-mobile web application**, authenticated by magic link, with no public pages, no marketing site, no sharing, and no analytics. Primary navigation is fixed at six destinations:

**Study Queue · Study Items · Professional Profile & CVs · Job Analyses · Interview Notes · Settings**

Theory, LeetCode, System Design and Behavioral are **categories of StudyItem**, not sections of the app. AI is **contextual** to a CVPresentation, JobAnalysis or InterviewNote — never a menu item, never scheduled, and every proposal requires an explicit per-proposal accept or reject before anything changes.

Five screens are the system's proving ground: magic-link login · ranked Study Queue with score breakdown · typed StudyItem detail and review · Job Analysis with gaps and AI proposals · CVPresentation editor with preview.

---

## The direction: Reading Room

Warm paper, ink navy, one humanist sans, no cards. The page leads with a single decision and its written justification; everything else recedes into a quiet ranked list. It reads as a study desk, not a console.

Carried in from the other explorations: **the clarity of Direction A's ranked queue and score breakdown**, and **Direction B's density — for analytical tables only**, exposed as `data-density="dense"`.

- **`identity/ReadingRoom.html`** — the refined identity: AA-verified palettes, type scale, density comparison, the three wordmark proposals in light/dark/mono/favicon, icons, control states. **This is the approval document.**
- `directions/` — the original three-way exploration, kept as the record. `directions/index.html` notes the outcome.

---

## Content fundamentals

The product's own documentation defines its voice with unusual rigour; these rules are extracted from `CONTEXT.md` and the mockup's copy, not invented.

**Domain terms are exact, and there is an explicit banned list.** `CONTEXT.md` pairs every concept with an *Avoid* line. Use the left column; never the right.

| Say | Never say |
|---|---|
| StudyItem | topic, card, item, task |
| EffectiveScore | priority, rank, score |
| Mastery | skill level, proficiency, confidence |
| Demand | relevance, urgency, market demand |
| Importance | weight |
| ProfessionalProfile | CV, resume, profile |
| CVPresentation | CV version, regional CV, tailored resume |
| EvidenceLink | tag link, relevance link |
| AnalysisDraft | AI result, analysis output |
| JobGap | skill gap, missing skill |

In running UI copy the terms are written as two plain words in sentence case (“study item”, “effective score”), reserved for PascalCase only when naming the entity itself.

**Sentence case everywhere.** `Send sign-in link`, not `Send Sign-In Link`. The only uppercase in the system is the 11px mono micro-label (`EFFECTIVE SCORE`), tracked at 0.14em.

**Second person, no first person, no personality.** *“Know what to study next, and why.”* · *“Invite-only. Ask for access if you don't have one.”* The product never says “I”, never says “we”, never congratulates. There is no streak, no badge, no “Great job!”. A completed review results in the number changing.

**State the reason, always.** This is the product's central promise, so copy carries justification rather than assertion: *“Linked from two job analyses”*, *“Never reviewed”*, *“Average of your last three reviews”*. If a screen shows a ranking, it shows why.

**AI copy is cautious and attributed.** Anything AI-produced is a *draft* or a *proposal*, verbed with *propose* — never *recommend*, never *knows* — and always sits next to Accept / Reject. Trigger copy is explicit and costed: *“Analyse with AI”*, *“3.10 of 8.00 EUR used this month”*. Never “✨ AI-powered”, never “magic”.

**Punctuation and numerals.** Middle dot `·` separates metadata fragments; em dash `—` joins a title to its qualifier (*Ledgerline — Senior Backend Engineer*). Relative time in prose (*reviewed 11 days ago*), absolute on hover. All figures tabular. Scores are integers 0–100, ratings are `n of 5`, weights are `40/35/25`.

**No fictional data.** Mocks show only values the domain actually produces. **No commit hashes, no fake IDs, no invented telemetry** — the early exploration's `a3f91c2`-style metadata was decoration and has been removed. If a number would have to be invented to fill a slot, the slot is wrong.

**No emoji. Ever.** Nothing in the source repository contains one.

**Errors are literal and blameless.** *“That sign-in link has expired. Request a new one.”* — what happened, what to do, no apology theatre.

---

## Visual foundations

**Colour.** Two hand-built themes. Light: paper `#F6F3EC`, sheet `#FFFDF8`, ink `#1B1A17`. Dark: warm-neutral paper `#171614`, sheet `#1E1D1A`, ink `#F0EDE6` — the same paper at night, not a blue-black inversion. One accent, ink navy `#24405C` / `#8FB4D9`, used for links, primary actions, active navigation, the focus ring, and the single “AI draft awaiting confirmation” marker. Three semantics — brick / ochre / moss — used only for gap severity and matched requirements, always paired with a word so meaning survives greyscale. **Categories are never colour-coded.**

**Contrast.** Every text token clears WCAG AA at the size it is used, in both themes: ink 16.4:1, secondary 7.5:1, metadata 5.7:1 (light) and 15.1 / 7.0 / 5.1:1 (dark). Ratios hold on the tint surface too, which is the tighter ground — metadata measures 4.7:1 light and 4.8:1 dark there. Filled navy buttons are 10.1:1 light, 7.4:1 dark. `--border-strong` exists at 3.2–3.4:1 specifically for control edges, which the decorative hairlines cannot legally carry. Measured ratios are printed on the swatches in `identity/ReadingRoom.html`.

**Type.** `Public Sans` (variable, bundled) for everything; `IBM Plex Mono` (Regular + Medium, bundled) for digits and tracked uppercase micro-labels. Hierarchy is size, weight and space — never a second family, never colour. Scale: 11 · 12.5 · 13.5 · 15 · 15.5 · 16.5 · 19 · 25 · 30 · 42 · 52. **12.5px is the floor for any text a user reads**, and 11px mono labels are always uppercase and tracked; the 11px mono label is permitted only for tracked uppercase labels that duplicate information present elsewhere.

**Density.** Comfortable (52px rows) is the default and governs the queue, item detail and CV editor. `data-density="dense"` (30px rows, zebra tint, 13.5px titles) is opt-in **per region**, for analytical tables only — never a whole page, and never where a row is a mobile tap target.

**Spacing.** 4px base, scale 2 → 64. Content column caps at 820px, page padding 44 × 38. Controls are 32px (small) and 40px (default); mobile tap targets never below 44px.

**Corners.** 0–4px, and that is the whole story: 2px chips, 3px buttons and inputs, 4px maximum. Corners read as trimmed paper. A `rounded-2xl` card breaks the direction instantly.

**Backgrounds.** Flat colour only. No gradients, no imagery, no texture, no pattern, no glass, no blur, no `backdrop-filter`. The sheet sits one step off the paper and that step *is* the depth system.

**Borders and shadows.** Three rule weights, all 1px: `--border-soft` for list dividers, `--border` for structure, `--border-strong` for control edges. Never two rules adjacent. **Paper does not float** — there is exactly one shadow token, `--shadow-overlay`, and it belongs to dialogs, popovers and the mobile sheet. Nothing else casts one.

**Cards.** There are none. Lists are rows separated by `--border-soft`; groups are separated by whitespace. Where a bounded region is unavoidable (a preview pane, a dialog) it is a 1px rule at 3px radius with no fill change and no shadow.

**Motion.** Functional only, 80–180ms, `cubic-bezier(.2,0,.2,1)`. Colour and background transitions on state change; no entrances, no bounce, no spring, no skeleton shimmer — loading is a static muted row. `prefers-reduced-motion` zeroes every duration.

**Hover / press / disabled.** Hover darkens navy to `#16283A` (lightens to `#B0CBE6` in dark) — never opacity, never a lift. Ghost controls hover to `--surface-alt`. Press steps one value further with no transform. Disabled is 45% opacity plus `cursor: not-allowed`. Destructive actions are outlined in `--critical` and always require a second confirmation.

**Focus.** `2px solid var(--accent)` at `2px` offset on every interactive element in both themes — never removed, never replaced by a shadow ring. Full keyboard operation is a hard requirement, including the 1–5 rating control (arrow keys) and Accept/Reject proposal pairs.

**Imagery.** There is none, and that is the position: the product holds the user's private CV and interview history. The only image in the product is the CV photo, user-supplied and off by default per CVPresentation.

**Layout.** Fixed 200px sidebar on desktop, collapsing to a bottom bar under 768px. Single 820px content column; two-column detail views split content / aside. Page title, then a one-line statement of what is being shown and how it is sorted, then content. Numeric columns are right-aligned and tabular.

---

## Iconography

**The source repository contains no icons, no icon font, and no SVG assets.** Nothing was copied from it because there was nothing to copy.

**Substitution (flagged):** [Lucide](https://lucide.dev) — 25 glyphs, **copied into `assets/icons/` as files** (ISC licence included). No CDN. The stroke was rewritten from Lucide's default `2` to **`1.75` on the 24 grid** (≈1.17px optical at the 16px display size) so glyphs sit at the same weight as Public Sans Regular. **If you have a preferred set, say so and it will be swapped.**

**Usage:** load `assets/icons/icons.js` once per page — it injects the sprite inline so `currentColor` works — then `<svg class="icon"><use href="#icon-check"></use></svg>`. Individual files are also in `assets/icons/<name>.svg` for build pipelines that prefer them.

Rules, unchanged across the system:

- Icons appear in **navigation** and in **confirm/destructive affordances**. Nowhere else.
- Never next to a heading, never as a list bullet, never inside body copy, never decorating an empty state.
- Never coloured except by inheriting `currentColor` from an active nav item.
- 16px in navigation and buttons, 20px maximum.
- **No emoji as icons. No Unicode glyphs as icons** — with two deliberate exceptions: `←` in back links and `→` in “show all” links, which are text.
- The six navigation glyphs: `list-ordered` (Study Queue), `book-marked` (Study Items), `user-round` (Profile & CVs), `briefcase` (Job Analyses), `notebook-text` (Interview Notes), `settings` (Settings).

---

## Components

Twenty-three primitives, grouped by concern. The source repository defines no component library (`frontend/` is unstyled), so this inventory is derived from the five representative screens — every entry exists because one of them needs it. Each has a sibling `.d.ts` props contract and a `.prompt.md` with usage.

**`components/core/`** — `Icon` · `Button` · `IconButton` · `Chip` · `Badge` · `Callout` · `EmptyState` · `Tabs` · `Dialog`

**`components/forms/`** — `Field` · `Input` · `Textarea` · `Select` · `Checkbox` · `RatingScale`

**`components/navigation/`** — `Brand` · `SidebarNav` (exports `NAV_ITEMS`) · `PageHeader`

**`components/domain/`** — `ScoreNumeral` · `ScoreBreakdown` · `QueueRow` · `ProposalRow` · `DataTable`

Deliberately absent: Toast, Avatar, Accordion, Breadcrumb, Pagination, Card. Nothing in the product needs them, and a card component in particular would contradict the direction — the interface has rows and whitespace, not cards.

Domain-specific by design: `RatingScale` (the 1–5 Importance / InitialMastery / confidence control), `ScoreBreakdown` (the only chart in the product), `QueueRow`, `ProposalRow` (per-proposal Accept / Reject on an AnalysisDraft).

---

## Index

```
styles.css                  entry point — @import list only
tokens/
  fonts.css                 @font-face, local binaries
  colors.css                base ramp + semantic aliases, light and dark, ratios in comments
  typography.css            families, sizes, weights, tracking
  space.css                 4px scale, radii, control heights, DENSITY scale
  elevation.css             one overlay shadow + scrim
  motion.css                durations, easing, reduced-motion override
identity/
  ReadingRoom.html          the refined visual identity
  Wordmark.html             the approved Bookmark mark — construction and usage
components/
  core/ forms/ navigation/ domain/     23 primitives, each with .d.ts + .prompt.md
ui_kits/app/
  index.html                click-through recreation of the five screens
directions/
  index.html                the three-way exploration and its outcome
  DirectionA-CommitLog.html · DirectionB-Instrument.html · DirectionC-ReadingRoom.html
guidelines/                 foundation specimen cards
  colors-brand · colors-neutral · colors-semantic
  type-display · type-body · type-mono
  spacing · radii · elevation · focus · density
  icons · wordmarks
assets/
  fonts/                    Public Sans (variable) + IBM Plex Mono, with OFL licences
  icons/                    25 Lucide SVGs, sprite.svg, icons.js, ISC licence
  logo/                     three wordmark proposals — rule/ bookmark/ queue/
                            each with lockup light · dark · mono-ink · mono-paper
                            and favicon · favicon-dark · favicon-mono
                            plus symbol-*.svg (currentColor, one file per theme)
thumbnail.html              project tile
github.md                   source-repo association and sync record
SKILL.md                    agent-skill entry point
```

**Approved mark:** Bookmark. See `identity/Wordmark.html` for construction, clear space, the three size cuts and misuse rules. Lockup type is converted to paths — no font dependency.

---

## Intentional additions

- **Lucide icon set.** The source defines no iconography and a six-item navigation needs glyphs. Bundled locally, flagged above, trivially swappable.
- **Public Sans + IBM Plex Mono.** The source specifies system stacks only. Both are OFL, bundled as files, chosen for a humanist voice that holds from 52px numerals to 11px labels.
- **The Bookmark mark.** The source has no logo. Three proposals were shown; Bookmark was approved and refined — an off-centre squared notch and two descending cut slots, both drawn from the product rather than from decoration.
- **The density scale.** Not in any source document; added because the domain has genuinely dense screens (Study Items, job requirements, AI usage) that the airy default would stretch to three screenfuls.

## Known caveats

- **Lockup outlines are traced, not from a font-tool.** The wordmark paths were produced by contour-tracing Public Sans at 240px and simplifying to 0.05px of display error — visually exact, but if you have a type tool to hand, regenerating them from the real outlines would be tidier.
- **Contrast ratios are computed, not machine-audited.** They should be re-verified with an automated checker against the real components once those exist.
