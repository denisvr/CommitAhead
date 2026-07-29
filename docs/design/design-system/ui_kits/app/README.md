# CommitAhead — application UI kit

A click-through recreation of the five screens the design system is validated against. One product, one surface: a private, invite-only web application.

Open `index.html`.

| Screen | What it proves |
|---|---|
| Magic-link login | Brand at size, one field, one action, invite-only fine print |
| Study Queue | The ranked list, the "next" hero with a written justification, and the score breakdown |
| StudyItem detail | Typed category details (LeetCode here), mastery history, and the 1–5 review control |
| Job Analysis | Dense requirements table with gap severity, and per-proposal Accept / Reject on an AnalysisDraft |
| CVPresentation editor | Tabs, entry selection, per-presentation visibility rules, and a live preview |

## What is real and what is not

Screens compose the primitives in `components/` — nothing is re-implemented locally. Sample content is fictional (Ledgerline, Anchor Systems) and carries no invented identifiers: no commit hashes, no UUIDs, no telemetry. Every number shown is one the domain actually produces — EffectiveScore 0–100, mastery 1.0–5.0, importance 1–5, EvidenceLink weight 0–5.

Behaviour is cosmetic: navigation, tab switching, accept/reject, checkbox toggles and the theme switch work; nothing persists and no AI call is made. The "Analyse with AI" button shows the explicit-trigger and confirmation flow only.

## One constraint to respect

Every `.jsx` in this project is also concatenated into `_ds_bundle.js`, including these screen files. So they must have **no top-level side effects**: the React root is mounted from an inline block in `index.html` (inline scripts are not swept), and each file reads its primitives *inside* the component body rather than at module scope — a module-level read would resolve against a namespace that is still being populated when the bundle evaluates. None of these functions is `export`ed, so nothing here leaks into the design system's public namespace.

## Interactions worth trying

- Sign in (any email) → the queue.
- Click a queue row → the StudyItem detail; submit a review with the 1–5 scale.
- Job Analyses → accept or reject individual proposals, then apply the draft.
- Profile & CVs → switch between Content, Preview and Presentation settings.
- The sun/moon control at the bottom of the sidebar switches theme; both are first-class.
