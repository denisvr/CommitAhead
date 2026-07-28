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

### StructuredSuggestion command allowlist
**Needed for:** Phase 4
**Question:** Which source mutations are safe and valuable enough to be represented as typed commands in MVP?
**Minimum candidates:** AddJobRequirement, AddJobGap, UpdateCVPresentationSummary, AddInterviewGap, AddInterviewLesson
**Constraint:** Anything outside the explicit allowlist remains an AdvisorySuggestion and can never mutate a source automatically

---

## Domain / Architecture

### Typed detail storage strategy (StudyItem discriminated union)
**Needed for:** Phase 1
**Option A:** JSONB column on `StudyItems` — simple, no joins; queries on detail fields require JSON operators
**Option B:** Four dedicated 1:1 detail tables keyed by `study_item_id`; relational and strongly constrained but adds joins. This is explicit composition mapping, not EF inheritance/TPT/TPC.
**Affects:** `docs/architecture/persistence.md`, EF Core mapping, migration complexity, any query that filters by detail fields

### Tiebreaking for equal EffectiveScore
**Needed for:** Phase 1 (ranked-list query)
**Question:** When two StudyItems have identical EffectiveScore, what is the secondary sort key?
**Options:** `createdAt ASC` (oldest first), `title ASC` (alphabetical), `updatedAt DESC` (most recently touched), random (non-deterministic)
**Affects:** `docs/domain/model.md`, ranked-list query, integration tests for ordering

---

## Frontend

### CV export format
**Needed for:** Phase 5
**Options:** PDF (via headless Chrome/Puppeteer, or a .NET PDF library), DOCX, HTML (rendered in browser for printing)
**Constraints:** Markdown must be sanitised before HTML generation; locale formatting applied; page limit enforced
**Affects:** `docs/product/brief.md` MVP scope, export use case, visual regression fixture strategy

### Component library / UI framework
**Needed for:** Phase 1
**Options:** shadcn/ui + Tailwind CSS, Radix UI primitives, MUI, custom
**Constraints:** Must not require CDN resources (CSP `connect-src 'self'`); any bundled fonts must not require `font-src` exceptions; `unsafe-inline` styles must not be required (CSP `style-src 'self'` only)
**Affects:** CSP configuration (may require exceptions), bundle size

---

## Document Processing

### PDF text extraction library and hard limits
**Needed for:** Phase 3
**Question:** Which maintained .NET text-only PDF parser is used, and what exact timeout, memory, and page-count limits supplement the decided 5 MB / 50 000-character limits?
**Constraints:** No rendering, script execution, embedded-link fetching, OCR, or parser network access; malformed, encrypted, image-only, and over-limit documents are rejected
**Affects:** Infrastructure adapter, upload validation, container resources, security tests

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
