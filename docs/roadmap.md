# CommitAhead — Implementation Roadmap

The roadmap is organised as vertical slices. Every phase must leave a working, tested increment; infrastructure and domain layers are added only when the current slice needs them. A phase starts only after its blocking TBDs are decided and the previous phase's CI gates pass.

---

## Phase 0 — Secure Foundation *(complete — security/architecture baseline and E2E acceptance are all verified)*

**Outcome:** An authenticated, enabled user can run a production-shaped empty application shell locally; CI proves the architecture and security baseline.

The real Supabase Cloud project exists (`Devalente Org` / `CommitAhead`, West EU/Ireland), reserved for production only (Phase 6c) — development instead uses a fully local Supabase instance for Auth (ADR-0023, via `supabase start`), never Cloud. `Supabase:Url`/`Supabase:AnonKey` point at that local instance, while `ConnectionStrings:CommitAheadDb` points at the local Docker Postgres (`backend/docker-compose.yml`), which already has an enabled user's `users` row seeded. This is a deliberate, accepted split: development uses Supabase Auth (local, ADR-0023) for authentication while the application's own PostgreSQL stays entirely local via Docker — there is no need to develop against the real Postgres, or against Supabase Cloud at all, before deployment. Backend-mediated magic-link/PKCE auth, per-user authorization, CSRF, and security headers are implemented and verified end-to-end this way — including a real magic-link login → session → refresh → logout cycle against the local Supabase Auth API, both host-run and via `docker-compose.dev.yml` (ADR-0022).

**Applying the migration bundle and roles/RLS scripts to the real Supabase Postgres is deferred to internet deployment (Phase 6c)**, not blocking Phase 0 — it needs the project's real database password, which stays with the user (see `README.md`).

- [x] Create `.slnx` and Domain, Application, Infrastructure, and API projects (`backend/`)
- [x] Create React 19 + Vite + TypeScript project and a frontend component test (`frontend/`)
- [x] The E2E stack, reset path, and E2E-only auth endpoint exist and are verified (`npm run verify:foundation` in `e2e/`) — `docker-compose.e2e.yml` (proxy/app/db/db-init/external-stub, only `proxy` host-facing), `E2ESessionController`/`E2EConfigurationGuard`, `reset-db.mjs`, `run-full.mjs`, `playwright.config.ts`, and `tests/fixtures/e2e-test.ts`. Journey spec files `tests/journeys/001` and `004` are written and passing (journeys 002/003 were removed with Study/Job-Analysis/AI — see Phase 6b), in the canonical `e2e/` layout fixed by `docs/testing/strategy.md` §7.11; see Phase 6b below for the per-journey detail
- [x] Configure project references and the API composition root according to ADR-0013
- [x] Serve the production Vite build from Kestrel on the same origin (copied into the published artifact's `wwwroot` at publish time, not into the backend source tree)
- [x] Wire EF Core + Npgsql; a minimal `User` identity table (id, supabase_user_id, email, is_enabled, created_at_utc — ADR-0015) has a generated `InitialCreate` migration plus a follow-up migration adding a case-insensitive unique index on email. `commitahead_app` and a separate migration role (`backend/scripts/database/001_roles.sql`, `002_rls_users.sql`) are applied and verified end-to-end against a local Docker Postgres (`backend/docker-compose.yml`), via the reproducible `backend/scripts/setup-local-db.ps1` (roles → migrations → RLS in one command; `002_rls_users.sql` is idempotent)
- [x] Create the Supabase project
- [ ] *(Deferred to Phase 6c internet deployment, not blocking)* Apply the migration bundle and `001_roles.sql`/`002_rls_users.sql`/`004_rls_phase2.sql` to the real Supabase Postgres, and seed each enabled user's `users` row there — user-run, needs the real DB password
- [x] Implement backend-mediated magic-link/PKCE auth (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`), per-user authorization (ADR-0015), CSRF (`/auth/csrf` + validation middleware), and security headers
- [x] Closed login: `/auth/login` normalizes and validates the email, looks up a provisioned + enabled `User` (case-insensitive unique index), and only calls Supabase for a match — an unknown or disabled email gets the same generic response without ever reaching Supabase
- [x] Secure-by-default authorization: an `AuthorizationOptions` fallback (and default) policy requires authentication plus an enabled ADR-0015 user for every endpoint; only Health and the `/auth/*` endpoints carry `[AllowAnonymous]`
- [x] The ADR-0015 enabled-user check is an authorization policy/handler applied via that fallback policy, not global middleware — it never blocks `/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`, or `/auth/csrf`, and logout always clears cookies even if the external Supabase revoke call fails
- [x] Session hardening: the frontend does a single-flight refresh-and-retry on 401 (get CSRF → `/auth/refresh` → retry the original request once); the backend enforces an effective 15-minute access-token limit independent of the token's own `exp` (via its `iat` claim); the session-start timestamp is sealed with ASP.NET Data Protection so the 7-day absolute timeout holds even against a non-browser replay of a captured cookie; the frontend attempts a refresh before logout so `/auth/logout` has a live token to revoke
- [x] The SPA fallback route no longer swallows unmatched `/api` or `/auth` requests into `index.html` — they return a real 404
- [x] Add a minimal authenticated home screen (`GET /api/me` + a login form / signed-in view in `frontend/`)
- [x] Add OpenAPI generation (build-time, via `Microsoft.Extensions.ApiDescription.Server`) and generated TypeScript client compilation (`frontend/src/api/generated`)
- [x] Add the NetArchTest architectural rules (Domain/Application/Infrastructure dependency direction, controllers depend on Application only — enforced by two tests, including one preventing controllers from injecting repositories directly — and repository production implementations exist only in Infrastructure, checked against an explicit named list of persistence ports: `IUserRepository`, `IRlsSessionContext`, `IProfessionalProfileRepository`, `ICVPresentationRepository`)
- [x] Add blocking CI: Gitleaks and generated-client drift, on top of the build/format/lint/type-check/test/NuGet+npm audit gates already in place; `dotnet publish` now fails (not just warns) when `frontend/dist` is missing, and a dedicated CI job builds the frontend, publishes the backend, starts the published Kestrel app, and verifies `/`, `/api/health`, and that unmatched `/api`/`/auth` routes 404

**Exit criteria:** production builds run locally from Kestrel; unauthenticated/unknown-or-disabled-user/CSRF/header tests pass (locally-signed JWTs, per `docs/testing/strategy.md`); no frontend Supabase key exists. E2E acceptance passes — Journey 1 (`001-authenticated-access.spec.ts`) proves an unauthenticated visitor is kept out, a test-issued session is consumed and authorizes the app shell/`GET /api/me`, and logout ends the session. Phase 0 is fully complete.

---

## Phase 1 — Ranked Study Queue *(removed — 2026-08-18)*

The Ranked Study Queue (StudyItem, StudyReview, PriorityOverride, ScoringConfig,
EffectiveScorePolicy, the ranked-queue query, and their E2E journey) was fully implemented and
verified, then removed in the same scope reduction that dropped Job Analyses, Interview Notes, and
the AI analysis pipeline. See the `20260818163818_DropStudyJobAnalysesInterviewNotesAnalysisDraftsAndAI`
migration and `docs/current-state.md` for what replaced it. Nothing from this phase is part of the
current app.

---

## Phase 2 — Professional Profile and CV Editing *(complete — implementation and E2E acceptance both verified)*

**Outcome:** Canonical career data can be maintained once and curated into independently editable regional CVPresentations.

- [x] Implement ProfessionalProfile, ContactInfo, all seven canonical child collections, YearMonth, and skill-reference guards
- [x] Implement independent CVPresentation aggregate according to ADR-0012
- [x] Add canonical child tables, with skill references and CVPresentation's seven selections mapped as plain `uuid[]` array columns rather than the originally-planned FK-backed join tables (**deviation flagged, not silent — see ADR-0017**: an EF Core constructor-binding limitation, not a preference; the domain aggregate already fully enforces every invariant those join tables would have backed up at the DB level: skill references must exist, a referenced Skill can't be deleted, selection order is positional by construction). `CVPresentation` carries a composite FK on `(ProfessionalProfileId, OwnerUserId)` against a matching alternate key on `ProfessionalProfile`, making a cross-owner reference (invariant 29) impossible to persist, independent of the application-level check
- [x] Implement ProfessionalProfile CRUD; canonical-entry deletion removes affected CV selections (`DanglingSelectionCleanup`, run from every `Replace*UseCase`) and guards referenced Skills
- [x] Implement Create/Update/Delete/Get CVPresentation, including same-profile selection validation (invariant 23) and locale validation (an unrecognized `locale` is rejected at the domain level against the runtime's own culture list). `DeleteCVPresentationUseCase` is a plain single-aggregate delete — no cross-aggregate cleanup, no transaction wrapper needed
- [x] Grants and RLS for all nine Phase 2 tables (`004_rls_phase2.sql`): `professional_profiles`/`cv_presentations` scoped directly by `owner_user_id`, the seven canonical child tables scoped transitively through `professional_profile_id`, mirroring `003_rls_phase1.sql`'s `study_reviews` pattern
- [x] Build ProfessionalProfile editors, CVPresentation selection/reordering, and preview shell. **Formatting rules, scoped down deliberately:** the preview renders locale-aware month/year via `Intl.DateTimeFormat` (the substantive part of "formatting rules"), with a plain `YYYY-MM` fallback if a persisted locale predates the backend's own validation or isn't one `Intl.DateTimeFormat` recognizes; a presentation's free-text `dateFormat` pattern (e.g. `"dd MMM yyyy"`) is not parsed/applied literally — `YearMonth` has no day component, so a general date-pattern engine is disproportionate to this slice
- [x] Add persistence, use-case, API, and component tests, including selection ordering and FK behavior — covering RLS isolation for the Phase 2 tables (two-user isolation, denial without owner context, safe script reapplication), the CVPresentation→ProfessionalProfile cross-owner FK rejection, locale validation, and the selection-save race fix (functional state updates so a slower section's completion can't overwrite a faster sibling's). Component tests cover every new page's load/error states and the two new generic design-system components (`EntryListEditor`, `SelectionOrderEditor`); a live authenticated click-through was not possible in the dev sandbox used to build this phase (no way to complete the real Supabase magic-link flow there) — recommended as a manual pass before considering the phase fully verified end-to-end

**Exit criteria:** a CVPresentation can curate canonical entries without duplicating them; editing one presentation does not mutate another. The non-E2E half is verified by Layers 1–4 (domain/use-case/repository/API tests) and Layer 6 (frontend component tests, MSW-backed) — see `docs/testing/strategy.md`. Journey 4 (`004-cv-presentation-export.spec.ts`) exercises curating a CVPresentation's selections end to end through the real UI. Phase 2 is fully complete.

---

## Phase 3 — Evidence Sources *(removed — 2026-08-18)*

Job Analyses, Interview Notes, PDF upload/extraction, and EvidenceLinks were fully implemented and
verified, then removed in the same scope reduction. Nothing from this phase is part of the current
app.

---

## Phase 4 — Explicit AI Analysis *(removed — 2026-08-18)*

The `IAIProvider` abstraction, `AnthropicAIProvider`, AnalysisDraft/SuggestionProposal/LinkProposal/
StudyItemProposal, AIUsageRecord budgeting/idempotency, the three `AnalyzeX` use cases, and
`ApplyAnalysisDraft`/`DiscardAnalysisDraft` were fully implemented and verified, then removed in the
same scope reduction — along with the `"ai-analysis"` rate-limit policy and the now-dead
`IUnitOfWork`/`EfUnitOfWork` abstraction that existed only to support AnalysisDraft's cross-aggregate
cleanup. Nothing from this phase is part of the current app.

---

## Phase 5 — CV Export *(implemented and tested end to end for one template, including E2E acceptance and the post-merge visual-regression fixture — fully complete)*

**Outcome:** At least one regional CV template produces a verified downloadable document.

**Decide first:** ~~export format/engine~~ — decided (ADR-0020: PDF via QuestPDF, docs/tbd.md).

- [x] Implement export renderer abstraction and one template — `IExportRenderer`/`CVExportDocument` (`backend/src/CommitAhead.Application/CVPresentations/CVExportDocument.cs`) is the layout-only port ADR-0020 describes; `Render` returns a `RenderedCVExport` (PDF bytes plus the renderer's own page count, computed internally via PdfPig — Application never references a PDF library). `QuestPdfCVExportRenderer` (`backend/src/CommitAhead.Infrastructure/CVPresentations/`) is the one A4 template, rendering every field `CVExportDocument` carries (contact/summary/experience incl. Client/education/skills/languages incl. Certification/certifications incl. ExpiresAt/CredentialId/Url/projects incl. Url/profile links), including nested Markdown bullet lists. `QuestPdfCVExportRendererTests` proves the rendered PDF (via PdfPig) contains every section's real content including bullet-list items and the previously-omitted fields, omits an excluded contact field, and overflows onto more than one page once content exceeds one page's worth.
- [x] Apply restricted Markdown rendering with a runtime-appropriate allowlist sanitizer — `RestrictedMarkdownParser` (Markdig-based, `backend/src/CommitAhead.Application/CVPresentations/RestrictedMarkdownParser.cs`) parses Markdown into a sanitised `MarkdownBlock`/`MarkdownRun` tree — no images, no raw HTML, links kept only for https/http/mailto — mirroring `RestrictedMarkdown.tsx`/`restrictedUrlTransform.ts`'s exact allowlist (threat-model.md's "CV/PDF export: same allowlist... no exceptions"). `RestrictedMarkdownParserTests` (12 tests) covers bold/italic, headings, bullet lists, allowed/disallowed link schemes, image stripping, and raw-HTML stripping.
- [x] Apply locale dates, visibility rules, selected-entry order, and page limit — `ExportCVPresentationUseCase` resolves every selection in order against the owner's `ProfessionalProfile` (dictionary-lookup pattern mirrored from `AnalyzeCVPresentationUseCase`), applies `IncludeEmail`/`IncludePhone`/`IncludeAddress`, rejects an unsupported `TemplateKey` explicitly (`ExportCVPresentationOutcome.UnsupportedTemplate` — only `ExportCVPresentationUseCase.SupportedTemplateKey` ("modern-one-page") actually renders), and rejects `IncludePhoto=true` explicitly (`UnsupportedPhoto` — no photo upload/storage path for `ContactInfo.PhotoStorageKey` exists anywhere in this codebase, so export must not silently ignore the flag), formats `YearMonth` dates locale-aware (`CVExportDateFormatter`, backend counterpart of `formatYearMonth.ts`, same fail-soft fallback to `yyyy-MM`), and enforces `PageLimit` as a hard cap against the renderer's own reported page count (`ExportCVPresentationOutcome.PageLimitExceeded` if exceeded — QuestPDF's own layout has no page-count constraint to enforce mid-render, so this is a post-render check the renderer computes and the use case compares, not a rejection during layout, and not something QuestPDF itself enforces). `ExportCVPresentationUseCaseTests` covers not-found, another owner's presentation, the missing-profile invariant guard, visibility-flag application, dangling-selection skipping, the unsupported-template and unsupported-photo outcomes, and both sides of the page-limit check.
- [x] Add ExportCVPresentation use case/controller and download UI — `GET /api/cv-presentations/{id}/export` on `CVPresentationController` (`[UsesOwnerScopedData]`, returns `application/pdf`, 404, or 409), covered by `CVPresentationEndpointTests` (unauthenticated, not-found, and a full round trip asserting the real returned PDF's parsed text). Frontend: a "Download PDF" button on `CVPresentationDetailPage`'s header (`exportCVPresentation` in `api.ts`, using openapi-fetch's `parseAs: 'blob'` so the existing 401-refresh middleware still applies) triggers a synthetic-anchor Blob download on success, and shows an inline message for `PresentationNotFound`/`PageLimitExceeded`/`UnsupportedTemplate`/`UnsupportedPhoto` (the last two read `error.outcomeCode` off the 409 ProblemDetails body) — covered by component tests (successful download via a patched `URL.createObjectURL`/`revokeObjectURL`, the page-limit-exceeded message, and a generic-failure message), all against a real MSW-mocked `Response`. The `CVPresentationForm`'s Template field is a disabled single-option control (not free text) and its "Include photo" checkbox can only be unchecked, never checked, since neither is actionable yet. A presentation saved before this restriction (or with a hand-edited `TemplateKey`) can't be fixed through the disabled select itself, so the form separately shows an inline warning naming the unsupported value plus a "Use the default template" action that sets `TemplateKey` back to `modern-one-page` in local form state only — nothing is persisted until the user explicitly saves — covered by a component test that renders a legacy `TemplateKey`, applies the correction, and asserts the saved request body.
- [x] Add parsed-output assertions on every PR — `RestrictedMarkdownParserTests`, `ExportCVPresentationUseCaseTests`, `QuestPdfCVExportRendererTests`, and `CVPresentationEndpointTests`' export test all assert against real parsed values (PdfPig-extracted text, or the resolved `CVExportDocument`), never a snapshot/golden file.
- [x] Add one deterministic visual-regression fixture per template post-merge — `QuestPdfCVExportRenderer.RenderPageImages` rasterises each page as PNG from the exact same document tree `Render` turns into PDF bytes (`QuestPDF`'s own `GenerateImages`, fixed at 144 DPI — never a second, independently-maintained rendering path), and `QuestPdfCVExportRendererVisualRegressionTests` diffs the one committed baseline for `modern-one-page` (`backend/tests/CommitAhead.Infrastructure.Tests/CVPresentations/VisualBaselines/`) against a fresh render with a tolerant per-pixel comparison (not byte-for-byte PNG equality — Skia's own anti-aliasing can shift a pixel or two at glyph edges between runs even with identical layout). Verified the fixture actually catches a regression, not just reliably passes: temporarily widened a section heading's font size, confirmed the test failed with a clear "N/M pixels (X%) differ..." message, then reverted. Regenerating the baseline after an intentional template change is a separate `[Fact(Skip = ...)]` test (`RegenerateBaseline_ModernOnePage`), run explicitly by name and reviewed by eye before committing the new PNG — nothing writes a baseline as a side effect of an ordinary test run. Pinned the test-only image-decoding dependency to `SixLabors.ImageSharp` 3.1.12 rather than the current 4.x, which requires a build-time Six Labors license key even for free-tier-eligible use — not worth taking on for a PNG-decode-and-diff helper that never ships in the production image.

**Exit criteria:** E2E Edit → Export CV passes; parsed output proves required text, exclusions, ordering, locale, and page limit; the one committed visual baseline matches a fresh render within tolerance. Journey 4 (`004-cv-presentation-export.spec.ts`) passes the Edit → Export half end to end through the real UI (a real Playwright `download` event, `%PDF-` magic bytes); the parsed-output half (required text, exclusions, ordering, locale, page limit) is deliberately proven at Layers 1–4 via PdfPig instead, per `docs/testing/strategy.md` §7.10 — a second, independent PDF-parsing stack in Playwright would just add noise, not signal. Phase 5 is now fully complete.

---

## Phase 6 — Production Hardening *(6a local production-like runtime implemented and infrastructure-verified; 6b local Playwright journeys explicitly invoked only, both remaining journeys implemented and passing; 6c internet deployment explicitly deferred, not started)*

**Outcome:** The complete MVP is safely deployable to the internet.

**Decide first:** hosting/secrets platform, Data Protection key storage, backup retention/restore cadence, and log retention — all explicitly deferred to Phase 6c below; Phase 6 starts instead with a hosting-neutral local Docker deployment (ADR-0021) to validate the container itself before choosing where it runs.

Split into three explicitly separate tracks, per the current priority: get the local
production-like runtime solid first (6a); run the local, isolated Playwright E2E journeys only
when explicitly invoked (6b) — never automatically, never as part of ordinary PR validation, and
never as a substitute for internet-deployment work; do not start internet deployment work (6c)
until that is explicitly decided.

### Phase 6a — Local Production-Like Runtime *(implemented and infrastructure-verified)*

**Outcome:** The complete app (API + built SPA + local PostgreSQL) runs reliably on a developer's
own machine in a production-like way — real Docker image, reproducible migrations/RLS, real
Supabase Auth against whatever project is configured, data persisting across ordinary restarts,
secrets outside the image, and zero automatic external calls — with startup/health/logs/shutdown/
reset all documented. Not an internet deployment and not evaluated as one.

- [x] Build reviewed EF migration bundle and production container — `Dockerfile` (repo root) is a multi-stage build (Node frontend build → pinned `.NET SDK 10.0.302` publish → minimal ASP.NET Core runtime as a non-root user), built and verified locally with `docker build`. `docker-compose.prod.yml` runs it alongside a dedicated PostgreSQL, both restart-safe via named volumes, both bound to `127.0.0.1` only (not the whole LAN), with `app` carrying a `deploy.resources.limits` (1 CPU / 1 GiB) making the threat model's own "container limits are the real backstop" claim actually true. `backend/scripts/build-migration-bundle.ps1` produces the self-contained EF migration bundle (`dotnet ef migrations bundle --self-contained`) as the portable artifact for a target without the .NET SDK; `backend/scripts/setup-production-db.ps1` mirrors `setup-local-db.ps1` (roles → migrations → RLS) against this stack's own Postgres for local validation, using `dotnet ef database update` directly since the SDK is already present on the developer's machine, and now rejects the `change_me` placeholder credentials `.env.production.example` ships rather than silently bootstrapping on them. `backend/scripts/bootstrap-production-user.ps1` seeds/updates exactly one local `users` row after migrations (never a Supabase account, never public signup) — without it, closed login (ADR-0015) has no enabled user to ever match on a fresh database. The Supabase magic-link callback is now configurable per environment (`Auth:CallbackUrl`, sent as a percent-encoded `redirect_to` query parameter on `/auth/v1/otp` — GoTrue reads it only from the query string, never the JSON body) rather than never sent at all — `http://localhost:5120/auth/callback` for local dev, `http://localhost:8080/auth/callback` for this stack, both required in the Supabase project's redirect allow-list.
- [x] Configure durable Data Protection keys (local Docker only; hosting secrets remain deferred) — `AddCommitAheadSecurity` persists the key ring to a configurable path (`DataProtection:KeyRingPath`) via `PersistKeysToFileSystem`; `docker-compose.prod.yml` backs it with a named volume, so cookie-encryption keys survive a container restart. Proven with a real test (`DataProtectionKeyPersistenceTests`): a payload protected by one `IServiceProvider` is unprotected by a second one pointed at the same path, the closest in-process stand-in for a restart. Keys are **not** encrypted at rest yet — that needs a cloud KMS, still open in `docs/tbd.md`. `ASPNETCORE_ENVIRONMENT=Docker` is a new environment name (ADR-0021) that skips `UseHsts()`/`UseHttpsRedirection()` only for this one hosting-neutral local stack, which has no TLS termination of its own; every other environment is unchanged. Auth/CSRF cookies needed no code change — they already read `Secure=true` unconditionally, and browsers treat `http://localhost` as a secure context regardless of scheme, so they are still sent to this stack at `http://localhost:8080`.
- [x] Configure Dependabot for NuGet, npm, Docker, and GitHub Actions — `.github/dependabot.yml` covers all four ecosystems (`/backend` for NuGet, `/frontend` for npm, repo root for Docker and GitHub Actions), each on a weekly schedule.
- [x] Pin Actions to SHAs and minimise workflow permissions — already true since CI was first added: every `uses:` step in `.github/workflows/ci.yml` is pinned to a full commit SHA (with a version comment for readability), and the workflow's only `permissions` block is the top-level `contents: read`, inherited by every job with no broader scope added anywhere.
- [x] Document and empirically verify everyday local operations — README.md "Production (Local Docker)" "Everyday operations" now documents `logs`/`down`/`up` (restart)/`down -v` (clean reset), and states the `--env-file` requirement on every `docker compose ... -f docker-compose.prod.yml` subcommand. **What was actually verified**, end to end against a disposable local `.env.production` with placeholder Supabase configuration: Docker build and startup; the `/api/health` endpoint and the built SPA being served; migrations/RLS applying via `setup-production-db.ps1` and the one enabled user seeding via `bootstrap-production-user.ps1`; the database and Data Protection named volumes persisting data across a `down` (no `-v`)/`up -d` restart; clean-reset behaviour (`down -v`); and that no automatic Supabase call ever happens (validated lazily, per request — the app started and served every check above with a placeholder Supabase URL). **What this did not prove**: real Supabase magic-link login/logout, or any authenticated end-user journey (Professional Profile, CVPresentation, export) — those need a real Supabase project, which this pass deliberately did not use. README.md's "Manual acceptance checklist" names exactly what to click through by hand once real credentials are configured.

**Exit criteria:** one documented Compose workflow reliably starts, stops, restarts (with data
persisting), and cleanly resets the complete local stack; migrations/RLS apply reproducibly;
secrets stay outside the image; zero automatic external calls — all verified empirically above.
Real authentication and authenticated end-user journeys are not part of this exit criterion; they
are the manual acceptance checklist's job (README.md). Not an internet-facing deployment and does
not imply one.

### Phase 6b — Local Playwright E2E Journeys *(explicitly invoked only — both remaining journeys implemented and passing)*

**Outcome:** The approved Playwright journeys pass against the isolated, non-persistent local
E2E stack (`docker-compose.e2e.yml`, `e2e/`) — a separate environment from Phase 6a's
production-like runtime, never started automatically and never part of ordinary PR validation.

- [x] Journey 1 (`001-authenticated-access.spec.ts`) — implemented and passing. Verified via `npm run verify:foundation`, standalone (`playwright test 001-authenticated-access.spec.ts`), and via the guaranteed-teardown `npm run e2e:full`; the external stub recorded zero unexpected requests and the stack was fully removed afterward each time.
- [x] Journey 4 (`004-cv-presentation-export.spec.ts`) — implemented and passing. Seeds a ProfessionalProfile with one Experience entry via API (§7.9: legitimate setup, not the tested behavior), then creates a CVPresentation, adds the seeded entry to its selections, and exports entirely through the UI; a real Playwright `download` event is asserted (`suggestedFilename()` ends `.pdf`, `failure()` is `null`, the file read from `download.path()` is non-empty and begins with the `%PDF-` magic number) — no PDF content parsing in Playwright, per §7.10. Verified standalone, alongside Journey 1 standalone to confirm independence, and together via the guaranteed-teardown `npm run e2e:full`; the external stub recorded zero unexpected requests and the stack was fully removed afterward each time.

Journeys 2 (`002-study-queue-ranking.spec.ts`, Study) and 3 (`003-job-analysis-draft.spec.ts`, Job
Analysis/AI) were implemented and passing, then removed with their features in the same scope
reduction that dropped Phases 1, 3, and 4. The kept file numbering (1 and 4) is intentionally left
as-is rather than renumbered, to avoid rewriting journey history for no functional benefit.

Both remaining approved journeys are implemented and passing. Adding a new journey requires an
explicit product decision recorded here and in `docs/testing/strategy.md` Layer 7 (§7.1).

**Exit criteria:** both journeys pass, run explicitly and in isolation from both Phase 6a's
local runtime and Phase 6c's hosting/deployment implementation — 6b is not part of that
implementation work, but it **is** a prerequisite for internet release readiness: Phase 6c must
not go live before both journeys pass here.

### Phase 6c — Internet Production Deployment *(explicitly deferred — not started)*

**Outcome:** The complete MVP is safely deployable to the internet.

Deferred until there is an explicit decision to begin production deployment — hosting platform and
secrets management (the "Decide first" above) are the first open questions, not yet chosen.

- [ ] Generate SBOM and block deployment on high/critical Trivy findings
- [ ] Run OWASP ZAP baseline against staging
- [ ] Configure encrypted backups and complete a restoration test — target policy decided (30-day retention, quarterly restore test) but automated/encrypted implementation deferred to the cloud-deployment stage (needs Supabase Storage + Postgres coverage a local stack can't exercise). In the meantime, `backend/scripts/backup-production-db.ps1`/`restore-production-db.ps1` give the local Docker stack a real, tested, lossless manual command — `pg_dump --format=custom` run entirely inside the `db` container and copied out with `docker compose cp` (never through PowerShell's text pipeline, so accented/non-ASCII content round-trips exactly) to a timestamped `backend/backups/*.dump` file; restore runs `pg_restore --clean --if-exists` the same way, preserving ownership (`commitahead_migrator`) and grants (`commitahead_app`) exactly as dumped, stopping and restarting the `app` service around the restore — not an automated system, not encrypted, not scheduled.
- [ ] Complete the pre-internet-deployment security checklist

**Exit criteria:** every MVP completion criterion in `docs/product/brief.md` is met and the production deployment passes its security gates. Phase 6a's local Docker deployment is a validation step toward that, not the exit criterion itself — hosting platform, secrets management, encrypted-at-rest Data Protection keys, automated backups, and centralized log retention are all still open (`docs/tbd.md`) and gate the actual internet-facing deployment.
