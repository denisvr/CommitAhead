# CommitAhead — Implementation Roadmap

This roadmap reflects the confirmed architecture. No phase should begin until the previous phase's CI gates pass.

---

## Phase 0 — Foundation (current)
**Goal:** Project skeleton, tooling, and CI baseline in place before domain logic is written.

- [ ] .NET solution: `.sln` + four `.csproj` files (Domain, Application, Infrastructure, Api)
- [ ] Vite + React 19 + TypeScript project (`src/CommitAhead.Web`)
- [ ] Test project structure (four test projects mirroring source layers)
- [ ] EF Core + Npgsql wired up; `CommitAheadDbContext` scaffold
- [ ] Supabase project created; PostgreSQL connection verified
- [ ] First EF Core migration (empty baseline)
- [ ] NetArchTest project with the five architecture rules
- [ ] CI pipeline: `dotnet build --warnaserror`, `vite build`, `dotnet format`, ESLint, `tsc --noEmit`
- [ ] Dependency scanning: `dotnet list package --vulnerable`, `npm audit`
- [ ] Gitleaks configured
- [ ] OpenAPI generation script + TypeScript client generation in CI

---

## Phase 1 — Domain and Core Persistence
**Goal:** All domain aggregates implemented and round-trippable in PostgreSQL.

- [ ] Domain entities: `StudyItem` (with typed details union), `StudyReview`, `ProfessionalProfile` (all canonical collections), `CVPresentation`, `JobAnalysis` (`JobSource` union, `JobRequirement`, `JobGap`), `InterviewNote`, `AnalysisDraft` (with typed proposal collections), `EvidenceLink`
- [ ] All domain invariants enforced in the domain layer
- [ ] Value objects: `PriorityOverride`, `JobSource`, `ContactInfo`, `YearMonth`
- [ ] EF Core mappings for all aggregates (including polymorphic references, typed details strategy — resolve TBD)
- [ ] Database migrations for all tables, constraints, and indexes
- [ ] Repository implementations for all aggregates
- [ ] `PriorityScoringService` (resolves ScoringConfig; applies formula)
- [ ] Ranked-list query (joins StudyReview + EvidenceLink; computes mastery, demand, effectiveScore)
- [ ] Domain unit tests (all invariants from `docs/domain/model.md`)
- [ ] Repository / integration tests (round-trips, constraints, ranked-list query)

---

## Phase 2 — Study Queue Features
**Goal:** Core preparation loop working end-to-end.

- [ ] Use cases: `CreateStudyItem`, `UpdateStudyItem`, `ArchiveStudyItem`, `DeleteStudyItem`
- [ ] Use case: `SubmitStudyReview`
- [ ] Use case: `GetRankedStudyQueue`
- [ ] Use cases: `SetPriorityOverride`, `ClearPriorityOverride`
- [ ] Use cases: `UpdateScoringConfig`, `ResetScoringConfig`
- [ ] API controllers for all of the above (thin, feature-folder)
- [ ] Auth middleware: PKCE callback, session cookies, JWT validation, `sub == OWNER_USER_ID`, CSRF
- [ ] Security headers middleware
- [ ] Rate limiting middleware
- [ ] Application use-case tests (fakes)
- [ ] API tests (auth, CSRF, CSP, validation, happy paths)
- [ ] React study queue UI: ranked list, StudyItem detail, typed detail forms, tag input, score breakdown display
- [ ] Frontend component tests (Vitest + RTL + MSW)

---

## Phase 3 — Evidence Sources and EvidenceLinks
**Goal:** Job analyses and interview notes feed the study queue via confirmed EvidenceLinks.

- [ ] Use cases: `CreateJobAnalysis`, `UpdateJobAnalysis`, `DeleteJobAnalysis` (with EvidenceLink cascade)
- [ ] Use cases: `CreateInterviewNote`, `UpdateInterviewNote`, `DeleteInterviewNote`
- [ ] Use case: `ManuallyCreateEvidenceLink`, `DeleteEvidenceLink`
- [ ] ProfessionalProfile use cases: all CRUD for canonical collections, `CreateCVPresentation`, `UpdateCVPresentation`, `DeleteCVPresentation`
- [ ] PDF upload endpoint: validation, quarantine key, text extraction, Storage upload
- [ ] API controllers for all of the above
- [ ] React UI: JobAnalysis form (paste + upload), InterviewNote form, ProfessionalProfile sections, CVPresentation editor

---

## Phase 4 — AI Integration
**Goal:** All three AI analysis commands working with the real provider abstraction.

- [ ] `IAIProvider` interface finalized
- [ ] `FakeAIProvider` with six scenario fixtures per command
- [ ] AI provider adapter (provider TBD — see `docs/tbd.md`): request construction, structured output, token limits, error mapping
- [ ] Use case: `AnalyzeJobAnalysis` (with budget reservation, idempotency, one-in-flight guard)
- [ ] Use case: `AnalyzeCVPresentation`
- [ ] Use case: `AnalyzeInterviewNote`
- [ ] Use case: `ApplyAnalysisDraft` (atomic fan-out; per-proposal decisions)
- [ ] `AIUsageRecord` persistence
- [ ] AI adapter unit tests (stubbed HTTP)
- [ ] React UI: AnalysisDraft review (per-proposal accept/reject), trigger analysis buttons

---

## Phase 5 — CV Export
**Goal:** CVPresentations can be exported in at least one format.

- [ ] Export format decided (TBD — see `docs/tbd.md`)
- [ ] CV export use case and controller
- [ ] Markdown sanitisation in export pipeline (DOMPurify + allowlist)
- [ ] Locale formatting (date format, personal-details rules)
- [ ] Page-limit enforcement
- [ ] Parsed content assertions in CI
- [ ] Visual regression fixture (one per template) — post-merge

---

## Phase 6 — Security Hardening and Pre-deployment
**Goal:** All security controls in place; pre-internet-deployment checklist completed.

- [ ] OWASP ZAP baseline integrated (staging environment)
- [ ] Trivy image scan integrated (deployment pipeline)
- [ ] SBOM generation automated
- [ ] Dependabot configured for all ecosystems
- [ ] GitHub Actions pinned to SHA; workflow token permissions minimised
- [ ] Pre-internet-deployment security checklist completed
- [ ] All four Playwright E2E journeys passing post-merge
- [ ] Live AI smoke test workflow created (manual trigger, explicit cost ceiling)
