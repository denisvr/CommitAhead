---
status: accepted
date: 2026-08-12
---

# CV export format is PDF, generated in-process with QuestPDF

## Context

`docs/tbd.md`'s "CV export format" entry blocked all of Phase 5 (`ExportCVPresentation`) — the
candidates were PDF (via a headless browser like Puppeteer/Playwright, or a .NET PDF library),
DOCX, or HTML rendered client-side for the browser's own print-to-PDF. `CVPresentation` already
carries every rule an export must respect (ADR-0012): locale dates, visibility flags for personal
details, selected-and-ordered canonical entries, a page limit, and an optional Markdown summary
override that must go through the same restricted-Markdown sanitisation the rest of the app already
uses (threat-model.md's "AI-generated Markdown content: same pipeline, no exceptions" — a CV
summary can originate from an accepted `UpdateCVPresentationSummary` suggestion, ADR-0005).

The export use case runs entirely server-side (`ExportCVPresentationUseCase`, Application layer) —
Phase 5's own exit criterion is a downloaded, parseable document proving required text, exclusions,
ordering, locale, and page limit, which a client-side print dialog cannot guarantee or test
deterministically in CI.

## Decision

**Format:** PDF. **Engine:** QuestPDF (MIT-based Community license, free for this project's
single-operator/single-user-today posture per its own revenue threshold — ADR-0015), called
directly from a new `IExportRenderer`/renderer implementation in Infrastructure, composed
declaratively (QuestPDF's own C# fluent layout API) rather than through an HTML/CSS template
rendered by a browser engine.

`ExportCVPresentationUseCase` builds the same minimised, rule-applied projection the frontend
preview already needs (selected entries in their saved order, visibility-filtered contact fields,
locale-formatted dates, the resolved summary Markdown) and hands it to the renderer; the renderer
owns only layout and page-limit enforcement, never business rules.

## Why

- **No new runtime dependency beyond a NuGet package.** QuestPDF is a managed .NET library with no
  external process, browser binary, or native dependency to provision, build, or patch — it runs
  identically in local dev, CI, and whatever Phase 6 eventually hosts on, with no extra attack
  surface (ADR-0009's CI posture already assumes a plain .NET test run, no headless-browser step).
- **Deterministic and directly parseable in tests**, matching the roadmap's own exit criterion
  ("parsed output proves required text, exclusions, ordering, locale, and page limit") — QuestPDF's
  own text-extraction/inspection API (or a small `PdfPig`-based read-back, already a dependency per
  ADR-0010) lets Api.Tests assert on the actual rendered content without a browser round-trip or a
  golden-file screenshot diff for every PR (visual-regression fixtures stay a deliberately separate,
  post-merge-only gate per the roadmap).
- **Matches this project's existing PDF posture.** `PdfPig` (ADR-0010) already reads PDFs
  server-side for job-posting uploads; QuestPDF writing them keeps every PDF touchpoint in managed
  .NET code, no second toolchain to reason about.
- **Page-limit enforcement is a first-class concern**, and QuestPDF's layout engine reports overflow
  explicitly (a document that doesn't fit throws rather than silently clipping or reflowing
  unpredictably) — closer to the domain's own hard page-limit rule than a browser's print engine,
  which has no comparable API.

## Consequences

- `docs/tbd.md`'s "CV export format" entry is resolved.
- QuestPDF's Community license requires attribution (a small notice, per its own terms) and caps
  free use by the *company's* prior-year gross revenue, not the app's; this project's own posture
  (ADR-0015: "today there is exactly one real user", not a commercial product) stays comfortably
  under that threshold — revisit only if that changes.
- Every visual layout choice (fonts, spacing, per-template structure) lives in C# code, not
  HTML/CSS — a frontend contributor comfortable with CSS Modules (ADR-0016) will find QuestPDF's own
  fluent API unfamiliar; this is an accepted trade-off for the determinism/dependency benefits above.
- Templates are added as new QuestPDF component classes in Infrastructure, one per target
  market/style (Phase 5's own exit criterion only requires the first one) — no templating language
  or file-based template engine is introduced.
- If a future template needs layout QuestPDF's model genuinely cannot express (e.g. a
  photo-heavy multi-column design), the fallback is a headless-browser renderer for that specific
  template only, not a wholesale re-decision of this ADR.

## Considered alternatives

- **Headless Chrome/Playwright, rendering an HTML/CSS template to PDF** — the most design-flexible
  option (reuses real CSS, easiest to make look polished), but adds a browser binary to every
  environment that must render a CV (local dev, CI, and Phase 6's eventual host), a second software
  supply chain to patch, and non-trivial startup cost per render; rejected primarily for the
  dependency/CI-determinism cost, not layout capability.
- **DOCX (via OpenXML or a similar library)** — lets the user keep editing after export, but DOCX
  layout primitives are considerably more manual to compose correctly (no fluent layout API
  comparable to QuestPDF's), and a CV export's primary use case (attaching to an application, or
  printing) doesn't need post-export editability; rejected as solving a need this project doesn't
  have yet.
- **HTML rendered client-side, printed via the browser's own print dialog** — zero new backend
  dependency, but produces no real downloadable artifact the backend can serve, log, or test — it
  cannot satisfy the roadmap's own "parsed output" exit criterion, and print-CSS fidelity varies by
  browser; rejected as not actually implementing `ExportCVPresentation` as a use case.
