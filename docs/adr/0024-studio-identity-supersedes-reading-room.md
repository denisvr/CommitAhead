# ADR-0024: Studio identity supersedes Reading Room

**Status:** Accepted

> **Partially superseded.** The "Consequences" section below said Studio's `AppShell` would be
> header-only, no sidebar — true when written, no longer true. A collapsible left Sidebar (Home,
> Professional profile, CV presentations) and a circular AccountMenu were added afterward at the
> user's explicit request, modelled on Azure DevOps's own nav rail. See
> `docs/design/design-system/components.md` ("AppShell", "Sidebar", "AccountMenu") for the current
> shape; this document still records why Studio itself was chosen over Reading Room.

**Supersedes:** the identity half of ADR-0016 (its implementation decision — React components with
CSS Modules and shared tokens — is unchanged and still binding).

## Context

Reading Room was approved before any real feature screen existed. It was a calm, document-like
identity: warm paper ground, ink navy accent, no cards, no shadows, 0–4px radii, icons forbidden
beside headings, separation by whitespace and a single hairline.

Two things then happened.

The product narrowed to Professional Profile & CV presentations only (see `docs/roadmap.md`), and
the master profile turned out to be the whole application rather than one page among six. That page
is a dense workspace: eight sections, unbounded achievement lists, a live document preview, and
per-entry status. It is not a reading surface.

Prototyping that page produced three candidate directions. The approved one
(`docs/design/prototypes/cv-3-studio-v2.1.html`) is a workspace of discrete cards on a cool neutral
ground with an indigo accent. A faithful Reading Room rendering of the same layout was also built
(`cv-4-reading-room.html`) so the choice was made against two real screens rather than against a
description. Studio was chosen.

## Decision

Studio is the approved identity for the CommitAhead frontend.

What changes from Reading Room:

- **Ground and accent.** Cool neutral page with a white card in light, near-black page with a slate
  card in dark; indigo accent. Warm paper and ink navy are gone.
- **Cards are real.** A card is a named structural surface with a border, a 12px radius and a
  shadow. Reading Room explicitly had no card component; Studio's page is built from them.
- **Radii up to 14px**, assigned by role, replacing the 0–4px ceiling.
- **Shadow exists**, for cards, the preview sheet, the floating control and overlays.
- **Icons beside section headings are allowed** — one per section, from the approved Lucide set.
  Reading Room forbade this. Eight stacked sections on one page need a scannable anchor, and that is
  the sole exception; every other icon rule carries over unchanged.
- **Type scale rebalanced** for a denser workspace, with the readable floor raised from 12.5px to
  12px and 11.5px reserved for tracked uppercase labels.

What is kept, deliberately:

- **The Bookmark mark.** The prototype used a placeholder "C" tile. There is no reason to discard an
  approved mark with a complete asset set, and "the place you have reached in your preparation" still
  describes the product. The symbol is tinted with `--accent`; the existing lockup SVGs are
  Reading-Room-coloured and need recolouring when the shell is implemented.
- **Public Sans and IBM Plex Mono**, bundled locally, no CDN. The prototype used a system stack only
  because it was a standalone file.
- **Colour means severity or accent**, never category.
- **Motion is functional only**, and `prefers-reduced-motion` still flattens it.
- **Every production restriction** in `readme.md`: no inline styles, no CDN assets, no injected
  markup, no imports from `docs/design/`.

## Dark mode

Reading Room had a dark theme; Studio must have one of equal quality, and the requirement was
explicit: as crisp and as clearly delimited as light.

The two themes are therefore built on one relationship rather than as independent palettes — the
page ground is one step away from the card, and the card is where content lives. Light puts a white
card on a grey page; dark puts a lighter slate card on a near-black page.

Depth is the one thing expressed differently, because a shadow carries almost nothing on a dark
ground. `--card-border` resolves to the soft hairline in light, where the shadow does the work, and
to the stronger line in dark, where the edge does. Components reference that token and never either
literal value.

Dark is authored twice — once for an explicit choice, once for the system preference — because those
are genuinely different code paths, and the system-preference path is where a missing token shows
up. `verify-contrast.mjs` fails if the two blocks drift.

## Consequences

- `docs/design/design-system/` is rewritten: `readme.md`, `components.md`, `page-patterns.md` and
  all five token files. `identity/Wordmark.html` remains authoritative for mark construction — the
  mark did not change. `identity/ReadingRoom.html` is historical only.
- `frontend/src/design-system/tokens/` is now a copy of the reference tokens, carrying a header that
  says so. The reference is the source of truth and both sides move in the same PR.
- Every component consumed semantic aliases rather than raw ramp values, which is why the re-theme
  was almost entirely a token swap. Four names needed attention: `--text-headline` was dropped from
  the scale (`EmptyState` now uses `--text-lead`), `--content-max` kept its meaning as the reading
  column and gained `--page-max` beside it, `--surface-alt` was kept rather than renamed, and
  `--section-gap` kept its name with a Studio-appropriate value.
- `AppShell` was restructured: at the time of this decision, Studio had no sidebar, so the shell was
  header-only and owned the theme control (**since superseded — see the banner at the top of this
  document**; a Sidebar and AccountMenu were added later). A side effect worth having regardless —
  the Reading Room shell rendered its nav and logout twice (desktop sidebar plus mobile bottom bar),
  which forced `getAllByRole(...)[0]` in tests because jsdom cannot hide either copy. Controls now
  render once.
- Contrast is verified mechanically rather than asserted in a comment.
  `node docs/design/design-system/verify-contrast.mjs` reads the tokens and exits non-zero on
  failure. Running it caught three real defects that visual review had missed: metadata text at
  4.06:1 on the page ground, a control border at 1.57:1 against a 3:1 requirement, and three tokens
  present in the explicit dark block but absent from the `prefers-color-scheme` one — so anyone on
  "match system" in dark would have got light-theme values for them. All three are fixed.
- `--border-strong` exists because of the second of those defects and is now used by every control
  edge (`Input`, `SelectionOrderEditor`, and others added since). Decorative separators keep
  `--border`. (`TagInput` itself was later removed along with the ProfessionalProfile editor
  redesign that replaced it with `CollapsibleRow`-based sections; the token it used lives on.)

## Alternatives considered

**Keep Reading Room and apply it to the approved layout.** Built and reviewed as
`cv-4-reading-room.html`. It is a coherent screen and costs no documentation churn, but it forbids
the section icons and the card structure that make eight stacked sections legible, and it reads as a
document when the page is a workspace.

**Adopt Studio's colours but keep Reading Room's shape rules.** Rejected as the worst of both: the
0–4px radius and no-shadow rules are what make Reading Room calm, and stripping them from Studio
while keeping its palette leaves an identity with no consistent logic behind it.
