# CommitAhead — Solution Architecture

## Overview

```
Browser (React 19 + Vite production build served by Kestrel)
  │  OpenAPI-generated TypeScript client
  │  HttpOnly session cookies (Secure, SameSite=Strict)
  ▼
ASP.NET Core 10 Web API  ──────────────────────────────────┐
  │ Controllers (thin, one per feature folder)              │
  │ Middleware: auth validation, CSRF, error mapping,       │
  │            structured logging, rate limiting            │
  │                                                         │
  │ Application layer                                       │
  │   Feature-folder use case classes                       │
  │   Repository interfaces (IStudyItemRepository, …)      │
  │   IAIProvider interface                                 │
  │                                                         │
  │ Domain layer                                            │
  │   Aggregates, value objects, invariants                 │
  │   EffectiveScorePolicy (pure formula + invariants)      │
  │                                                         │
  │ Infrastructure layer                                    │
  │   EF Core 10 + Npgsql (CommitAheadDbContext)           │
  │   Repository implementations                            │
  │   ProviderAIAdapter : IAIProvider  (provider TBD)      │
  │   PDF text extractor                                    │
  │   Supabase Storage client                               │
  └──────────────────────────────────────────────────────┘
        │                    │                    │                    │
   PostgreSQL          Supabase Auth        Supabase Storage     AI Provider API
   (Supabase)          (JWKS + magic        (private bucket;     (provider/model
                        link + PKCE)         backend-only)         TBD)
```

## Layer Responsibilities

### Domain (`CommitAhead.Domain`)
- Aggregates, value objects, enums, domain invariants
- `EffectiveScorePolicy`: pure formula and validation over already-resolved ScoringWeights; it performs no persistence or configuration lookup
- No dependencies on frameworks, EF Core, ASP.NET, or Supabase
- Contains repository interfaces? **No** — repository interfaces live in Application

### Application (`CommitAhead.Application`)
- One use case class per operation (`CreateStudyItemUseCase`, `ApplyAnalysisDraftUseCase`, …)
- Repository interfaces (`IStudyItemRepository`, `IJobAnalysisRepository`, …)
- `IAIProvider` interface
- Orchestrates domain objects and repositories; contains no EF Core or HTTP concerns
- Returns result objects (not domain aggregates) to the API layer

### Infrastructure (`CommitAhead.Infrastructure`)
- `CommitAheadDbContext` (EF Core 10 + Npgsql)
- Repository implementations
- `ProviderAIAdapter : IAIProvider` (renamed after provider selection; see `docs/tbd.md`)
- PDF text extraction (text-only library; no rendering)
- Supabase Storage client (file upload, quarantine key generation, delete)
- ASP.NET Data Protection key ring configuration

### API (`CommitAhead.Api`)
- Thin controllers: bind request → call use case → map result to HTTP response
- Middleware pipeline: auth validation (JWT `sub` must resolve to an existing, enabled application `User` — see ADR-0015), CSRF, error mapping, structured logging, rate limiting
- OpenAPI / Swagger generation (source for TypeScript client)
- Auth endpoints: PKCE callback, refresh, logout
- Composition root: references Infrastructure only from startup/DI registration; controllers never resolve Infrastructure types directly
- No business logic; no direct repository or DbContext access

### Frontend (`frontend/`)
- React 19 + TypeScript + Vite
- OpenAPI-generated TypeScript client (regenerated and compiled in CI)
- Feature-folder component structure (mirroring backend features)
- Custom production components implemented incrementally with CSS Modules and shared CSS
  custom-property tokens (ADR-0016)
- Reading Room + Bookmark design contract from `docs/design/design-system/`
- MSW for component test isolation
- No Supabase SDK; no direct AI calls; all API calls go through the generated client
- No UI framework, CSS-in-JS, inline style attributes, CDN assets, runtime-injected SVG sprites,
  or design-prototype code
- Production assets are built by Vite and served by Kestrel from the same origin as the API; the local Vite development origin is explicitly allowlisted only in Development

## Key Flows

### Ranked Study Queue Load
1. Controller calls `GetRankedStudyQueueUseCase`.
2. Application resolves ScoringWeights from the optional override or code defaults and passes them to `IRankedStudyQueueQuery`.
3. The Infrastructure query joins `StudyReview` and `EvidenceLink`, applies the domain-defined formula using the supplied weights, and orders by EffectiveScore plus the configured deterministic tiebreaker.
4. Returns ordered list of StudyItem projections with computed fields.

### AI Analysis Command (e.g. AnalyzeJobAnalysis)
1. Controller validates CSRF + auth; checks idempotency key.
2. Use case acquires the global AI-call slot, checks the pending-draft guard, then atomically creates a Reserved AIUsageRecord after checking daily/monthly budget and the unique idempotency key.
3. Prepares input: extracts `JobSource.extractedText`; fetches minimal ProfessionalProfile skills summary + compact StudyItem catalogue.
4. Calls `IAIProvider.AnalyzeJobAnalysisAsync(input)` with configured token limits.
5. Provider returns structured proposals; use case validates IDs, enums, weights, and lengths.
6. In one database transaction, creates the `AnalysisDraft` (Pending) and reconciles the AIUsageRecord to Completed with actual provider usage and `analysisDraftId`; provider failures transition the reservation to Failed.
7. Returns the draft to the controller. Repeating a completed idempotency key returns the existing draft without another provider call.

### Apply AnalysisDraft
1. Controller calls `ApplyAnalysisDraftUseCase(draftId, decisions[])`, providing exactly one Accepted/Rejected decision per proposal and a complete user-finalised payload for every accepted actionable proposal.
2. Use case loads draft; asserts status = Pending.
3. Within a single DB transaction:
   - Validate that every proposal appears exactly once and that accepted final payloads are valid.
   - Preserve immutable proposed payloads and persist final Accepted/Rejected statuses plus separate accepted payloads.
   - Accepted `LinkProposal`s → create `EvidenceLink`s (validates uniqueness).
   - Accepted `StudyItemProposal`s → create `StudyItem`s.
   - Accepted `StructuredSuggestion`s → fire the typed domain command (e.g. `AddJobRequirement`).
   - Mark draft status = Applied.
4. Returns applied draft.
