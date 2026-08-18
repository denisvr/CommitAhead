# CommitAhead — Product Brief

## Purpose

CommitAhead is a private, invite-only web application for maintaining one canonical professional profile and curating it into locale-specific, exportable CV presentations. It answers the question: *what does my CV look like for this market, and can I get a document out of it?*

## Users

Public signup is disabled; accounts are provisioned out-of-band (see `docs/tbd.md`). Today the only account belongs to Denis Silva. Every user's data is isolated by owner from the start (see ADR-0015), so additional invited users can be added later without a rearchitecture. There is no sharing between users and no public pages.

## Design Principles

1. **Minimal complexity.** Defer everything not needed for the first production-ready cycle. If a feature requires explaining why it belongs in MVP, it probably does not.
2. **Private by default.** No product analytics or third-party telemetry. Minimal metadata-only operational logs are retained under the security policy. No external data sharing occurs.

## MVP Scope

The MVP delivers one production-ready cycle for professional profile management:

| Area | What it covers |
|---|---|
| **Professional Profile** | One canonical record — experience, education, skills, languages, certifications, projects, and links |
| **CV Presentations** | Curated, ordered, locale-specific views over the profile for multiple target markets, exported as PDF |

## MVP Completion Criteria

The MVP is complete when:
- A ProfessionalProfile and its canonical collections can be fully maintained (create, edit, delete).
- At least one CVPresentation can be created, have its selections curated and ordered, edited, and exported to a PDF that reflects its formatting rules (visibility flags, locale, page limit).
- The security controls described in `docs/security/threat-model.md` are in place.
- All CI quality gates pass (see `CLAUDE.md`).
- A pre-internet-deployment security checklist has been completed.
