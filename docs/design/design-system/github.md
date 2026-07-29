repo: denisvr/CommitAhead
branch: main
path: docs/

## Last sync

date: 2026-07-29T15:31:00Z

### Updated in this project

- Direction chosen: **Reading Room** (original design work, not from the repo). `tokens/` now ships it — warm paper, ink navy, Public Sans, plus a density scale for analytical tables.
- All colour tokens re-derived to clear WCAG AA at their used size in both themes; `--border-strong` added at 3:1 for control edges.
- Fonts and icons bundled locally from `google/fonts` (Public Sans, IBM Plex Mono — OFL) and `lucide-icons/lucide` (25 glyphs — ISC). No CDN references remain.
- **Bookmark** approved and refined as the mark; wordmark type converted to outlined paths (no font dependency).
- 23 components authored in `components/` (core, forms, navigation, domain) with props contracts and usage notes.
- Click-through UI kit of the five representative screens in `ui_kits/app/`.

## Screen map

| Project screen | Built from |
|---|---|
| `identity/ReadingRoom.html` | original; vocabulary and values from `CONTEXT.md`, `docs/domain/model.md` |
| `directions/DirectionA-CommitLog.html` | `docs/design/mockup.html`, `docs/design/visual-identity.md` |
| `directions/DirectionB-Instrument.html` | original; vocabulary from `docs/domain/model.md`, `docs/product/brief.md` |
| `directions/DirectionC-ReadingRoom.html` | original; superseded by `identity/ReadingRoom.html` |
| `tokens/*.css` | original (Reading Room); domain constraints from `docs/domain/model.md` |
| `identity/Wordmark.html` | original; the approved mark |
| `components/**` | original; props and enums from `docs/domain/model.md`, vocabulary from `CONTEXT.md` |
| `ui_kits/app/**` | original; screen inventory from `docs/design/visual-identity.md` ("Reference screens"), data shapes from `docs/domain/model.md` |
| `guidelines/*.card.html` | derived from `tokens/` |
| `assets/fonts/*` | google/fonts@main — `ofl/publicsans`, `ofl/ibmplexmono` |
| `assets/icons/*` | lucide-icons/lucide@main — `icons/`, stroke rewritten to 1.75 |

## Sync history

- 2026-07-29T14:44:21Z — Reading Room adopted; fonts and icons bundled locally from google/fonts and lucide-icons/lucide; AA corrections.

- 2026-07-29T14:12:02Z — initial read of `docs/design/`, `CONTEXT.md`, `docs/domain/model.md`, `docs/product/brief.md`; Direction A carried in verbatim as one of three explorations.
