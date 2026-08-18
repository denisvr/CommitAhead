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
  │   Repository interfaces (IProfessionalProfileRepository,│
  │   ICVPresentationRepository)                            │
  │                                                         │
  │ Domain layer                                            │
  │   Aggregates, value objects, invariants                 │
  │                                                         │
  │ Infrastructure layer                                    │
  │   EF Core 10 + Npgsql (CommitAheadDbContext)           │
  │   Repository implementations                            │
  │   QuestPdfCVExportRenderer : IExportRenderer (ADR-0020) │
  └──────────────────────────────────────────────────────┘
        │                    │
   PostgreSQL          Supabase Auth
   (Supabase)          (JWKS + magic
                        link + PKCE)
```

## Layer Responsibilities

### Domain (`CommitAhead.Domain`)
- Aggregates, value objects, enums, domain invariants
- No dependencies on frameworks, EF Core, ASP.NET, or Supabase
- Contains repository interfaces? **No** — repository interfaces live in Application

### Application (`CommitAhead.Application`)
- One use case class per operation (`CreateProfessionalProfileUseCase`, `ExportCVPresentationUseCase`, …)
- Repository interfaces (`IProfessionalProfileRepository`, `ICVPresentationRepository`)
- Orchestrates domain objects and repositories; contains no EF Core or HTTP concerns
- Returns result objects (not domain aggregates) to the API layer

### Infrastructure (`CommitAhead.Infrastructure`)
- `CommitAheadDbContext` (EF Core 10 + Npgsql)
- Repository implementations
- `QuestPdfCVExportRenderer : IExportRenderer` (CV PDF export, ADR-0020)
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
- No Supabase SDK; all API calls go through the generated client
- No UI framework, CSS-in-JS, inline style attributes, CDN assets, runtime-injected SVG sprites,
  or design-prototype code
- Production assets are built by Vite and served by Kestrel from the same origin as the API; the local Vite development origin is explicitly allowlisted only in Development

## Key Flows

### Professional Profile Update
1. Controller calls the relevant use case (e.g. `ReplaceExperienceUseCase`), scoped to the authenticated request's `OwnerUserId`.
2. Use case loads the owner's `ProfessionalProfile`, applies the domain-validated replacement, and persists it via `IProfessionalProfileRepository`.
3. `DanglingSelectionCleanup` removes any now-invalid entry ID from every `CVPresentation`'s selection arrays for that owner, so canonical edits never leave a presentation referencing a deleted entry.
4. Returns the updated projection to the controller.

### CV Presentation Export
1. Controller calls `ExportCVPresentationUseCase(id)`.
2. Use case loads the `CVPresentation` and its owning `ProfessionalProfile`; rejects unsupported templates or an unsupported photo request before rendering.
3. Resolves the presentation's ordered selections against the profile's canonical entries into a `CVExportDocument` (locale-formatted dates, sanitised Markdown, visibility flags applied).
4. Calls `IExportRenderer.Render(document)` (`QuestPdfCVExportRenderer` in Infrastructure); rejects the result if the rendered page count exceeds `PageLimit`.
5. Returns the PDF bytes to the controller for download.
