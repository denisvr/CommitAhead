# CommitAhead — Product Brief

## Purpose

CommitAhead is a private, single-user web application for structured technical interview preparation. It maintains a ranked study queue, a professional profile, and an evidence layer (job analyses, interview notes) that together answer the question: *what should I study next, and why?*

## Sole User

Denis Silva. No other users, no multi-tenancy, no sharing, no public pages.

## Design Principles

1. **Explicit AI.** AI is triggered only by deliberate user action on a specific evidence source. Nothing runs automatically or on a schedule.
2. **Human confirmation.** Every AI proposal requires explicit per-proposal acceptance before any domain state changes.
3. **Transparent prioritisation.** EffectiveScore is derived from three visible, understandable inputs — Importance, Demand, and Mastery gap — not a black box.
4. **Minimal complexity.** Defer everything not needed for the first production-ready cycle. If a feature requires explaining why it belongs in MVP, it probably does not.
5. **Private by default.** No product analytics or third-party telemetry. Minimal metadata-only operational logs and AI usage/cost records are retained under the security policy. No external data sharing occurs beyond the chosen AI provider, which must meet EU privacy requirements.

## MVP Scope

The MVP delivers one production-ready cycle across all six preparation areas:

| Area | What it covers |
|---|---|
| **Technical Theory** | Concepts, CAP theorem, SOLID, design patterns, etc. |
| **LeetCode** | Problems with approach notes, complexity analysis, optional C# solutions |
| **System Design** | Structured prompts, requirements, evaluation checklists, reference solutions |
| **Behavioral** | STAR stories mapped to competencies and question variants |
| **Professional Profile** | CV presentations for multiple target markets and locales |
| **Evidence Analysis** | Job postings and interview notes → EvidenceLinks → prioritised study queue |

## MVP Completion Criteria

The MVP is complete when:
- The study queue ranks items correctly using the EffectiveScore formula.
- All three AI analysis commands produce valid AnalysisDrafts and apply accepted proposals correctly.
- At least one CVPresentation can be edited and exported.
- The security controls described in `docs/security/threat-model.md` are in place.
- All CI quality gates pass (see `CLAUDE.md`).
- A pre-internet-deployment security checklist has been completed.
