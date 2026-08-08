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

### Default AI budgets
**Needed for:** Phase 4
**Question:** What billing currency and default daily/monthly ceilings are used, and may they be edited from the settings UI?
**Constraints:** Per-call token limits remain separate; budget checks include Completed actual cost plus active Reserved cost; provider/model pricing and currency must be versioned with the usage record
**Status:** Still incomplete/unenforced — `IAIUsageRecordRepository.GetSpentCostAsync` exists (owner-scoped, sums Completed actual cost plus active Reserved cost within a caller-supplied window) but `AnalyzeJobAnalysisUseCase` never calls it; no ceiling is checked before a reservation is allowed to proceed. The provider/model dependency this entry used to have on the resolved entry below is gone — Anthropic's own per-token pricing is the basis to price against — but the ceiling amounts and whether they're user-editable are still undecided.

### ~~AI provider selection~~ — decided
Resolved (ADR-0019): Anthropic, model Claude Haiku 4.5, called directly via the Messages API with
tool-use forced via `tool_choice` for structured output. Every `AnalyzeX` command uses this model;
a per-command override remains possible later (`IAIProvider.Describe(AiCommandType)` is already
commandType-scoped) without reopening this decision.

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
