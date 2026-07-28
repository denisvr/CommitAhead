# CommitAhead — Solution Architecture

## Overview

```
Browser (React 19 + Vite)
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
  │   PriorityScoringService                                │
  │                                                         │
  │ Infrastructure layer                                    │
  │   EF Core 10 + Npgsql (CommitAheadDbContext)           │
  │   Repository implementations                            │
  │   AnthropicAIProvider : IAIProvider  (provider TBD)    │
  │   PDF text extractor                                    │
  │   Supabase Storage client                               │
  └──────────────────────────────────────────────────────┘
        │                    │                    │
   PostgreSQL          Supabase Auth        Supabase Storage
   (Supabase)          (JWKS + magic        (private bucket;
                        link + PKCE)         backend-only)
                                                    │
                                          AI Provider API
                                          (TBD — Anthropic
                                           or equivalent)
```

## Layer Responsibilities

### Domain (`CommitAhead.Domain`)
- Aggregates, value objects, enums, domain invariants
- `PriorityScoringService`: resolves ScoringConfig (override or defaults) and computes EffectiveScore
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
- `AnthropicAIProvider : IAIProvider` (or equivalent; see `docs/tbd.md`)
- PDF text extraction (text-only library; no rendering)
- Supabase Storage client (file upload, quarantine key generation, delete)
- ASP.NET Data Protection key ring configuration

### API (`CommitAhead.Api`)
- Thin controllers: bind request → call use case → map result to HTTP response
- Middleware pipeline: auth validation (`sub == OWNER_USER_ID`), CSRF, error mapping, structured logging, rate limiting
- OpenAPI / Swagger generation (source for TypeScript client)
- Auth endpoints: PKCE callback, refresh, logout
- No business logic; no direct repository or DbContext access

### Frontend (`CommitAhead.Web`)
- React 19 + TypeScript + Vite
- OpenAPI-generated TypeScript client (regenerated and compiled in CI)
- Feature-folder component structure (mirroring backend features)
- MSW for component test isolation
- No Supabase SDK; no direct AI calls; all API calls go through the generated client

## Key Flows

### Ranked Study Queue Load
1. Controller calls `GetRankedStudyQueueUseCase`.
2. Use case calls repository which executes the ranked-list query (joins `StudyReview`, `EvidenceLink`, applies ScoringConfig weights, orders by EffectiveScore DESC or PriorityOverride.score).
3. Returns ordered list of StudyItem projections with computed fields.

### AI Analysis Command (e.g. AnalyzeJobAnalysis)
1. Controller validates CSRF + auth; checks idempotency key.
2. Use case acquires global AI-call slot (one in-flight limit); checks pending draft guard; checks daily/monthly budget reserve.
3. Prepares input: extracts `JobSource.extractedText`; fetches minimal ProfessionalProfile skills summary + compact StudyItem catalogue.
4. Calls `IAIProvider.AnalyzeJobAnalysisAsync(input)` with configured token limits.
5. Provider returns structured proposals; use case validates IDs, enums, weights, and lengths.
6. Creates `AnalysisDraft` (status = Pending) with typed proposal collections.
7. Records `AIUsageRecord` (metadata only — no prompt/response content).
8. Returns draft to controller; controller returns 201 with draft body.

### Apply AnalysisDraft
1. Controller calls `ApplyAnalysisDraftUseCase(draftId, acceptedProposalIds)`.
2. Use case loads draft; asserts status = Pending.
3. Within a single DB transaction:
   - Accepted `LinkProposal`s → create `EvidenceLink`s (validates uniqueness).
   - Accepted `StudyItemProposal`s → create `StudyItem`s.
   - Accepted `StructuredSuggestion`s → fire the typed domain command (e.g. `AddJobRequirement`).
   - Mark draft status = Applied.
4. Returns applied draft.
