# CommitAhead — Current State

This is the operational handoff for a new development session. It records only the current status,
priority, and document routing; detailed rules remain in their authoritative documents.

## Current implementation state

- **2026-08-18 scope reduction:** the app was cut down to a single feature area. Study (StudyItem,
  StudyReview, PriorityOverride, ScoringConfig, EffectiveScorePolicy), Job Analyses, Interview
  Notes, the explicit AI analysis pipeline (`IAIProvider`, `AnthropicAIProvider`, AnalysisDraft and
  its three proposal types, AIUsageRecord budgeting/idempotency), and EvidenceLinks were all
  removed — backend Domain/Application/Infrastructure/Api code, all their tests, and their
  frontend features. A single EF migration
  (`20260818163818_DropStudyJobAnalysesInterviewNotesAnalysisDraftsAndAI`) dropped the 13
  now-unused tables (`ai_usage_records`, `evidence_links`, `interview_notes`, `job_gaps`,
  `link_proposals`, `scoring_config_overrides`, `study_item_proposals`, `study_reviews`,
  `suggestion_proposals`, `job_requirements`, `study_items`, `analysis_drafts`, `job_analyses`).
  The RLS scripts under `backend/scripts/database/` went from 001–007 down to `001_roles.sql`,
  `002_rls_users.sql`, and `004_rls_phase2.sql` (the numbering gap is intentional — it is what the
  surviving script was always called). The Playwright E2E journeys for Study
  (`002-study-queue-ranking.spec.ts`) and Job Analysis/AI (`003-job-analysis-draft.spec.ts`) were
  removed with their features; `001-authenticated-access.spec.ts` and
  `004-cv-presentation-export.spec.ts` remain. Also removed as dead code once AnalysisDraft's
  cross-aggregate cleanup went away: `IUnitOfWork`/`EfUnitOfWork` (`DeleteCVPresentationUseCase` is
  now a plain single-aggregate delete, no transaction wrapper), the CVPresentation controller's
  `Analyze` action (backend-only, never wired to a frontend trigger), and the `"ai-analysis"`
  rate-limit policy.
- **What remains, and is implemented and verified:**
  - **Auth** — backend-mediated Supabase magic-link/PKCE login, refresh, logout, per-user
    authorization (ADR-0015), CSRF, and security headers. Untouched by the reduction above. Local
    development authenticates against a fully local Supabase instance (ADR-0023, via
    `supabase start`) — no Supabase Cloud project, credentials, or internet connection needed
    during development; Cloud is reserved for production only (Phase 6c, not started).
  - **Professional Profile** — the canonical CV data aggregate (`ProfessionalProfile`,
    `ContactInfo`, and its seven child collections: Experience, Education, Skill, Language,
    Certification, Project, ProfileLink) with full CRUD, skill-reference guards, and
    dangling-selection cleanup when a canonical entry is deleted.
  - **CVPresentation** — independent, locale-specific curated projections over a
    ProfessionalProfile (ADR-0012), with ordered selection collections, locale/template
    validation, and PDF export (`ExportCVPresentation`, QuestPDF via ADR-0020) including locale
    dates, visibility rules, page-limit enforcement, and a committed visual-regression baseline
    for the one supported template (`modern-one-page`).
- **Frontend shell, since ADR-0024 was first written:** a collapsible left Sidebar (Home,
  Professional profile, CV presentations — modelled on Azure DevOps's own nav rail,
  `localStorage`-persisted collapse state) and a circular AccountMenu replaced the original
  header-only shell; see the superseded-note banner at the top of ADR-0024 and
  `docs/design/design-system/components.md` ("AppShell", "Sidebar", "AccountMenu", "Home"). The
  ProfessionalProfilePage editor is Europass-style: each section defaults to read-only formatted
  text, not open input fields, with colour-coded Add (green)/Edit (accent)/Delete (red)/Done
  (green) actions (`components.md` "Button") and no separate Save button anywhere — every
  mutating action (add, edit, delete, reorder) persists immediately.
- **Manual reordering** for Experience, Education, Certifications, and Projects — a Move up/down
  pair (keyboard-accessible) plus a native HTML5 drag handle (mouse-only; **not** a JS drag
  library — this app's CSP has no `unsafe-inline` for `style-src`, which blocks the inline
  transform every such library uses for live drag feedback, see `CollapsibleRow.tsx`'s own header
  comment). Order is a real, persisted `Position` column on each of the four entities
  (`20260820112709_AddProfessionalProfileEntryPositions`), stamped from the client's array order on
  every `Replace*` and read back via an `ORDER BY` in `ProfessionalProfileRepository` — see
  `docs/architecture/persistence.md`. Skills, Languages, and ProfileLinks have no such ordering.
- A fully-containerized, hot-reload dev environment (ADR-0022) is implemented:
  `docker-compose.dev.yml` layers `db-init`/`api`/`frontend` onto `backend/docker-compose.yml`'s
  existing `db`. Phase 6a's persistent local production-like Docker runtime
  (`docker-compose.prod.yml`) is implemented and infrastructure-verified. Phase 6c, internet
  production deployment, has not started and is explicitly deferred.
- Both remaining Playwright E2E journeys pass: 1 (`001-authenticated-access.spec.ts`) and 4
  (`004-cv-presentation-export.spec.ts`), each verified standalone and together via the
  guaranteed-teardown `npm run e2e:full`, with zero unexpected external-stub requests and the
  stack fully removed afterward each time.

## Current priority

**Adopting the Devalente engineering standards (ADR-0025).** The canonical contract now lives in the
sibling `../engineering-standards` checkout; `CLAUDE.md` and `AGENTS.md` are discovery adapters and
`docs/engineering-context.md` holds this project's context. The gap analysis and phased plan are in
`docs/migration/engineering-standards-adoption-plan.md`.

Done: Phase 0 (adoption metadata, ADR-0025 through ADR-0028, ADR-0008 superseded) and the
package-independent part of Phase 1 (`NuGet.Config` for the private feed, explicit `[Authorize]` on
every protected operation, `AnalysisLevelSecurity=latest-all`).

Blocked on the private `Devalente.Shared.* 0.2.0` feed being restorable locally and in CI: the MVC
authorization-inventory test, and every code phase from the DbContext boundary onward (Phases 3-9).
Still open inside Phase 1 with no package dependency: transport and rate limits for the export and
write endpoints, and the security evidence register required by ADR-0027.

The feature work itself is complete: the Professional Profile / CVPresentation MVP (editing,
curation, export) and Auth are both implemented and E2E-verified. The Phase 6a manual acceptance
checklist (`README.md`, against `docker-compose.prod.yml` with real credentials) remains an optional,
not-yet-run manual pass — it does not block anything else.

Do not begin Phase 6c, choose hosting, or implement internet-deployment controls without explicit
user authorization. Do not re-introduce Study, Job Analyses, Interview Notes, AI analysis, or
EvidenceLinks without an explicit product decision to do so — they were deliberately removed, not
merely deferred.

## Environment boundaries

| Environment | Purpose | Persistence and external services |
|---|---|---|
| Normal development (host-run) | Fast feature iteration, IDE debugging | Local Docker PostgreSQL; local Supabase instance for Auth (ADR-0023, `supabase start`) — never Cloud during development |
| Normal development (containerized, ADR-0022) | Same as above, without host .NET SDK/Node.js; hot-reload | Same local Docker PostgreSQL/volume as host-run — shares data, not a separate environment; reaches the same local Supabase instance via `host.docker.internal` |
| Phase 6a local production-like | Exercise the deployable image locally as the user will normally run it | Persistent local Docker PostgreSQL and Data Protection volumes; configured real external services are request-driven only |
| Phase 6b E2E | Run the two approved Playwright journeys explicitly | Isolated, non-persistent Docker stack; local Supabase stub; no real external calls |
| Phase 6c internet production | Future internet deployment | Deferred; hosting and production controls remain undecided |

Never reuse the Phase 6a database for E2E and never treat the E2E stack as staging or production.

## Authoritative documents

- `docs/roadmap.md` owns implementation status and next work.
- `../engineering-standards/ENGINEERING.md` owns engineering rules (ADR-0025);
  `docs/engineering-context.md` owns this project's context and binding project decisions;
  `CLAUDE.md` and `AGENTS.md` are discovery adapters only.
- `docs/migration/engineering-standards-adoption-plan.md` owns the standards-adoption gap analysis
  and phase sequence.
- `CONTEXT.md` owns domain terminology.
- `docs/testing/strategy.md` Layer 7 owns E2E rules; `e2e/README.md` owns E2E commands.
- `README.md` owns local development, local production-like runtime, and manual acceptance commands.
- `docs/adr/` owns accepted architectural decisions.
- `docs/tbd.md` owns unresolved decisions. Never resolve one by assumption.

If documents conflict, use the owner above and correct the stale reference before implementation.
