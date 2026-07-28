# CommitAhead — Open Decisions (TBD)

Decisions that must be made before the affected phase begins. No decision here should be resolved by assumption — open a discussion, decide, document in the relevant ADR or doc, and remove the entry from this list.

---

## Infrastructure

### Hosting platform for ASP.NET Core API and React frontend
**Needed for:** Phase 0 / Phase 6
**Options:** Azure App Service, Railway, Fly.io, Render, self-hosted VPS, Docker Compose on a VM
**Constraints:** TLS required; environment variable injection required; container support preferred; single-process deployment; cost proportionate to a private single-user app
**Affects:** `docs/deployment/strategy.md`, Data Protection key ring configuration, secrets injection method

### Secrets management in production
**Needed for:** Phase 0 / Phase 6
**Options:** Azure Key Vault, Doppler, environment variables injected by the hosting platform, mounted secrets (Docker secrets)
**Depends on:** Hosting platform decision above

---

## AI Provider

### AI provider selection
**Needed for:** Phase 4
**Constraints:** Must provide EU-compliant privacy terms; training on submitted data must be opt-out or disabled; minimal data retention where supported; structured output (JSON schema enforcement) required
**Options:** Anthropic (Claude), OpenAI (GPT-4o), Google (Gemini), Azure OpenAI (EU data boundary)
**Affects:** `AnthropicAIProvider` naming (may change), prompt construction, token pricing for budget calculations, live smoke test parameters

---

## Domain / Architecture

### Typed detail storage strategy (StudyItem discriminated union)
**Needed for:** Phase 1
**Option A:** JSONB column on `StudyItems` — simple, no joins; queries on detail fields require JSON operators
**Option B:** Table-per-concrete-type — `LeetCodeDetails`, `SystemDesignDetails`, etc. as separate tables with 1:1 FK; relational but adds joins; EF Core table-per-type is supported
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
**Needed for:** Phase 2
**Options:** shadcn/ui + Tailwind CSS, Radix UI primitives, MUI, custom
**Constraints:** Must not require CDN resources (CSP `connect-src 'self'`); any bundled fonts must not require `font-src` exceptions; `unsafe-inline` styles must not be required (CSP `style-src 'self'` only)
**Affects:** CSP configuration (may require exceptions), bundle size

---

## Operations

### Data Protection key ring storage
**Needed for:** Phase 0 (production deployment)
**Question:** Where are ASP.NET Data Protection keys persisted across deployments?
**Options:** Azure Blob Storage, AWS S3, mounted volume, database table
**Depends on:** Hosting platform decision
**Affects:** Cookie encryption continuity across deployments; if keys rotate, all sessions are invalidated
