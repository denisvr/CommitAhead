# Out of Scope — MVP

The following are explicitly excluded. Implementing any of them would add complexity disproportionate to the MVP goal or contradict the design principles in `docs/product/brief.md`.

## Study Queue
- Spaced-repetition algorithm (SM-2 or similar)
- Automated archival based on mastery level
- Interview-date scheduling or countdown pressure
- Study streaks, gamification, or habit tracking
- Tiebreaking for equal EffectiveScore — TBD post-MVP (see `docs/tbd.md`)

## AI
- AI grading of LeetCode solutions
- AI generation of system design diagrams or complete code solutions
- Automatic tag synonym expansion (e.g. `"dp"` → `"dynamic-programming"`)
- Automatic retry of failed AI commands (every retry is an explicit user action)
- Scheduled or background AI analysis
- AI calls in automated CI (FakeAIProvider only — no exceptions)
- AI provider selection (TBD — see `docs/tbd.md`)

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
- Full penetration testing (deferred to pre-internet-deployment checklist)
- Scheduled or automatic live AI smoke tests
- DDoS-specific mitigations beyond rate limiting

## Infrastructure
- Multi-region deployment
- Offline / PWA mode
- Mobile app
- Export to external calendars or task managers
- Orphaned Storage file cleanup job (logged and accepted as eventual consistency for MVP)

## Analytics
- Usage dashboards, study-time tracking, or aggregate statistics
- Export to BI tools
