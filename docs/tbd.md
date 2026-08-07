# CommitAhead — Open Decisions (TBD)

Decisions that must be made before the affected phase begins. No decision here should be resolved by assumption — open a discussion, decide, document in the relevant ADR or doc, and remove the entry from this list.

---

## Users

### Invited-user provisioning
**Needed for:** whenever a second user is actually added (ADR-0015 makes the architecture ready for this; it does not schedule it)
**Question:** Since public signup stays disabled, how is a new invited user actually created — Supabase Admin API invite, a manual `User` row tied to a pre-created Supabase identity, an admin CLI/script?
**Constraints:** Must never require enabling public signup; a new user must get an isolated `OwnerUserId` scope with zero visibility into another user's data
**Affects:** ADR-0015, `docs/architecture/solution.md`, `docs/security/threat-model.md`

---

## Infrastructure

### Hosting platform for ASP.NET Core API and React frontend
**Needed for:** Phase 6
**Options:** Azure App Service, Railway, Fly.io, Render, self-hosted VPS, Docker Compose on a VM
**Constraints:** TLS required; environment variable injection required; container support preferred; single-process deployment; cost proportionate to a small, invite-only user base
**Affects:** `docs/deployment/strategy.md`, Data Protection key ring configuration, secrets injection method

### Secrets management in production
**Needed for:** Phase 6
**Options:** Azure Key Vault, Doppler, environment variables injected by the hosting platform, mounted secrets (Docker secrets)
**Depends on:** Hosting platform decision above

---

## AI Provider

### AI provider selection
**Needed for:** Phase 4
**Constraints:** Must provide EU-compliant privacy terms; training on submitted data must be opt-out or disabled; minimal data retention where supported; structured output (JSON schema enforcement) required
**Provider candidates:** Anthropic, OpenAI, Google, Azure OpenAI. Provider and model are selected separately using current privacy, structured-output, quality, latency, and pricing evidence at Phase 4.
**Affects:** `ProviderAIAdapter` final name and SDK, prompt construction, token pricing for budget calculations, live smoke test parameters

### Default AI budgets
**Needed for:** Phase 4
**Question:** What billing currency and default daily/monthly ceilings are used, and may they be edited from the settings UI?
**Constraints:** Per-call token limits remain separate; budget checks include Completed actual cost plus active Reserved cost; provider/model pricing and currency must be versioned with the usage record
**Depends on:** AI provider and model selection

### ~~StructuredSuggestion command allowlist~~ — decided
Resolved at Phase 4 kickoff to exactly the "minimum candidates" list, with no additions:
`AddJobRequirement`, `AddJobGap`, `UpdateCVPresentationSummary`, `AddInterviewGap`, `AddInterviewLesson`
(`StructuredSuggestionCommandType`, `backend/src/CommitAhead.Domain/AnalysisDrafts/`). A source
mutation not on this list can only ever be proposed as an AdvisorySuggestion, never applied
automatically. Extending the allowlist later is a normal backward-compatible enum addition, not a
breaking change — deferring the other four candidates' own command handlers to when their
Application-layer slice is built does not require reopening this decision.

---

## Frontend

### CV export format
**Needed for:** Phase 5
**Options:** PDF (via headless Chrome/Puppeteer, or a .NET PDF library), DOCX, HTML (rendered in browser for printing)
**Constraints:** Markdown must be sanitised before HTML generation; locale formatting applied; page limit enforced
**Affects:** `docs/product/brief.md` MVP scope, export use case, visual regression fixture strategy

---

## Operations

### Data Protection key ring storage
**Needed for:** Phase 6
**Question:** Where are ASP.NET Data Protection keys persisted across deployments?
**Options:** Azure Blob Storage, AWS S3, mounted volume, database table
**Depends on:** Hosting platform decision
**Affects:** Cookie encryption continuity across deployments; if keys rotate, all sessions are invalidated

### Backup retention and restore-test cadence
**Needed for:** Phase 6
**Question:** How long are encrypted database/Storage backups retained, and how often is a restoration test performed?
**Constraints:** Must cover PostgreSQL and private Storage; restored data must remain access-controlled and test artifacts must be deleted
**Depends on:** Supabase plan and hosting platform

### Production log retention
**Needed for:** Phase 6
**Question:** How long are metadata-only production logs retained?
**Constraints:** No user-authored content, request bodies, tokens, cookies, query strings, prompts, responses, or uploaded file content; access restricted to the owner/operator
