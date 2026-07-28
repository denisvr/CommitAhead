# CommitAhead

Private single-user interview preparation app. Full domain model and architecture confirmed — read `CONTEXT.md` for terminology and `docs/adr/` for why key decisions were made before suggesting alternatives.

## Stack
- **Frontend:** React 19 + TypeScript + Vite; OpenAPI-generated TypeScript client
- **Backend:** ASP.NET Core 10 Web API — Controllers, feature-folder use cases, no MediatR, no Minimal APIs
- **ORM:** EF Core 10 + Npgsql
- **Database:** PostgreSQL on Supabase
- **Auth/Storage:** Supabase (backend-mediated — anon key never sent to the browser)
- **AI:** `IAIProvider` abstraction; provider TBD; never called from frontend or domain layer

## Hard constraints
- No MediatR, no Minimal APIs, no generic `IUseCase<T>` interfaces
- AI commands produce `AnalysisDraft`s requiring human confirmation — AI never writes to domain entities directly
- Zero real AI calls in CI; `FakeAIProvider` only in automated tests
- Supabase anon key and all privileged keys are backend-only
- `EffectiveScore` is computed on-the-fly in the ranked-list query — not persisted

## Project structure (target)
```
CommitAhead/
├── CONTEXT.md                  ← domain glossary (read this)
├── docs/adr/                   ← architectural decisions (read before changing anything)
├── src/
│   ├── CommitAhead.Domain/
│   ├── CommitAhead.Application/
│   ├── CommitAhead.Infrastructure/
│   ├── CommitAhead.Api/
│   └── CommitAhead.Web/        ← Vite React frontend
└── tests/
    ├── CommitAhead.Domain.Tests/
    ├── CommitAhead.Application.Tests/
    ├── CommitAhead.Infrastructure.Tests/
    └── CommitAhead.Api.Tests/
```

## Test tooling
- Backend: xUnit, built-in assertions, WebApplicationFactory, Testcontainers.PostgreSql, Respawn, NetArchTest
- AI test double: `FakeAIProvider` (handwritten, deterministic, 6 scenario fixtures)
- Frontend: Vitest, React Testing Library, MSW, Playwright (E2E post-merge only)
