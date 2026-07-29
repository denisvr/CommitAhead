# CommitAhead — Visual Identity

**Status: proposed, not yet implemented.** This documents a draft visual identity generated as a design pitch (Claude Artifact). Nothing here is wired into `frontend/` yet — the running app still uses unstyled HTML. See `docs/design/mockup.html` for the visual reference; open it in a browser to see it rendered, including both light and dark themes.

## Concept

The system borrows structurally — not decoratively — from the product's own name and domain: `git`/commit vernacular (short monospace ids, "reviewed 3d ago" style metadata), a ranked queue where the row number is the actual priority order (not a decorative label), and color reserved for things that mean something (gap severity, the one brand accent) rather than spread across category tags for decoration.

Deliberately avoided: warm cream + serif + terracotta, near-black + acid-green, Inter/Space Grotesk as the default sans, `rounded-lg` everywhere, purple-to-blue gradient heroes — the common "AI-generated" look.

## Color

| Token | Purpose | Light | Dark |
|---|---|---|---|
| `--bg` | Page ground | `#F2F4F5` | `#12151A` |
| `--surface` | Cards, frames | `#FFFFFF` | `#191D23` |
| `--surface-alt` | Nav, panels | `#E7EAEC` | `#20252C` |
| `--border` | Hairline borders | `#D6DADD` | `#2B3038` |
| `--text` | Primary text | `#14181D` | `#EDEFF1` |
| `--text-muted` | Secondary text | `#4B535B` | `#A7AEB6` |
| `--text-faint` | Metadata, timestamps | `#7C848C` | `#6E767E` |
| `--accent` | Brand — links, primary actions, focus ring | `#106A64` | `#3FBDB4` |
| `--critical` | High-severity gap | `#B23A32` | `#E2695F` |
| `--caution` | Medium-severity gap | `#96691E` | `#D9A250` |
| `--good` | Matched requirement / high mastery | `#3D7F55` | `#6AB98A` |

The accent is a deep teal — not the green a "git/GitHub" association would default to (reserved instead for the `good` semantic), not a generic SaaS blue. Semantic colors (`critical`/`caution`/`good`) are a separate system from the brand accent on purpose: reusing the accent hue for a warning state would blur "this needs your attention" with "this is a CommitAhead action."

Both themes are tokens, not an inversion of one base palette — each shade was picked for contrast and legibility on its own ground.

## Typography

Three roles, three families, each with a reason:

| Role | Stack | Used for |
|---|---|---|
| Display | `'Iowan Old Style', Georgia, 'Times New Roman', serif` | Page titles, wordmark — a warm, literary serif contrasting the code-heavy content, evoking a considered "commit message" rather than sterile UI chrome |
| Body | `-apple-system, 'Segoe UI', 'Helvetica Neue', Arial, sans-serif` | UI chrome, labels, forms, body copy — a clean system stack, deliberately not a single signature typeface |
| Mono | `'SF Mono', 'Cascadia Mono', Consolas, 'Liberation Mono', monospace` | Scores, short ids, timestamps, anything tabular — genuinely functional here, not a "dev tool" affectation |

All three are system/fallback stacks (no `@font-face` embedding) so they render correctly cross-platform without a font file to maintain.

## Layout principles carried into the mockup

- Small corner radii (4–7px) throughout — deliberately not the soft `rounded-2xl` SaaS look.
- No decorative left-accent-bar-on-card pattern; flat list rows with hairline dividers instead.
- Category (LeetCode/System Design/Behavioral/Theory) is a plain monochrome outline chip, not a rainbow of tag colors — color stays reserved for severity and the brand accent.
- The rank number in the study queue is the literal `EffectiveScore` ordering, not a decorative numbered list.
- `font-variant-numeric: tabular-nums` wherever scores/dates line up in a column.

## Reference screens in the mockup

`docs/design/mockup.html` renders five screens against this system: `/login`, `/queue` (the ranked study queue), a `StudyItem` detail/review screen (with a mastery sparkline and confidence-rating control), `/profile` (ProfessionalProfile + CVPresentation note), and a JobAnalysis screen with AI draft proposals (accept/reject). These were chosen to stress-test the system across list, form, and card-heavy patterns — not as a complete screen inventory for Phase 1+.

## Open questions before implementation

- Not yet decided: component library / CSS approach for `frontend/` (Tailwind, CSS Modules, vanilla CSS with these tokens as custom properties, etc.) — see `docs/tbd.md` if this becomes a blocking decision for a Phase 1 UI slice.
- The mockup's fictional sample content (e.g. "Ledgerline", "Anchor Systems") is placeholder — not real companies, not the user's real CV data.
