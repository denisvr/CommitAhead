# Out of Scope — MVP

The following are explicitly excluded. Implementing any of them would add complexity disproportionate to the MVP goal or contradict the design principles in `docs/product/brief.md`.

## Study Queue, Job Analyses, and AI (removed, not merely deferred)

The Study Queue and the AI-assisted Job Analysis pipeline (spaced repetition, AI grading, AI
generation of diagrams/solutions, scheduled or background AI analysis, `FakeAIProvider`-gated CI
calls, and everything else that used to live under these two headings) were fully **removed** from
the codebase on 2026-08-18, not merely kept out of MVP scope — see `docs/current-state.md`. Do not
re-introduce either area without an explicit product decision to do so.

## Professional Profile
- Per-job CV versioning (CVPresentations reference canonical entries; no duplication)
- CV sharing or publishing to external platforms
- Collaborative feedback on CV content

## Job Pipeline
- Application tracking / status pipeline (Applied, Interviewing, Rejected, Offer)
- Company CRM or contact management
- Salary negotiation tracking
- Calendar integration

## Security
- Server-side JWT denylist (15-minute residual window after logout is accepted)
- Tagged-PDF accessibility
- Package-signature enforcement
- Full penetration testing (post-MVP; the pre-internet-deployment security checklist is still mandatory)
- Scheduled or automatic live AI smoke tests
- DDoS-specific mitigations beyond rate limiting

## Infrastructure
- Multi-region deployment
- Offline / PWA mode
- Mobile app
- Export to external calendars or task managers

## Analytics
- Usage dashboards, study-time tracking, or aggregate statistics
- Export to BI tools
