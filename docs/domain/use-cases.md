# CommitAhead — Use Cases

Key user journeys. Each maps to one or more application use case classes in the `Features/` folder structure. Every journey below operates entirely within the authenticated request's own data (ADR-0015) — the ProfessionalProfile and CVPresentations referenced anywhere in this document belong to that one user; there is no journey that reads or writes another user's data.

---

## 1. Authenticate

**Goal:** Sign in and establish a session.

1. Request a magic link (`LoginUseCase`, `/auth/login`) — only a provisioned, enabled `User` ever reaches Supabase; an unknown or disabled email gets the same generic response.
2. Follow the link back to the app (`CallbackUseCase`, `/auth/callback`) to establish the session.
3. The frontend transparently refreshes an expiring session (`RefreshUseCase`, `/auth/refresh`) via a single-flight refresh-and-retry on 401.
4. Sign out (`LogoutUseCase`, `/auth/logout`) — cookies are always cleared, even if the external Supabase revoke call fails.

---

## 2. Manage the Professional Profile

**Goal:** Keep the canonical CV data up to date.

1. Create or fetch the one-per-user ProfessionalProfile.
2. Edit its sections: add/edit/remove ExperienceEntry, EducationEntry, Skill, LanguageEntry, CertificationEntry, ProjectEntry, ProfileLink.
3. Deleting a canonical entry (e.g. a Skill) removes it from any CVPresentation selection that referenced it (`DanglingSelectionCleanup`, run from every `Replace*UseCase`); a Skill still referenced by an Experience or Project entry cannot be deleted until that reference is removed or reassigned.
4. Manually reorder Experience, Education, Certification, or Project entries (invariant 8) — every add/edit/delete/reorder persists immediately, with no separate save step.

---

## 3. Manage CVPresentations

**Goal:** Curate the canonical profile into an independently editable, locale-specific CV.

1. Create a CVPresentation for a target market (`CreateCVPresentation`), referencing the caller's own ProfessionalProfile.
2. Select and order canonical entries into the CVPresentation's seven selection collections (Experience, Education, Skill, Language, Certification, Project, ProfileLink); every selected ID must exist in the referenced ProfessionalProfile (invariant 4).
3. Set format rules: locale (validated against the runtime's own culture list), template, photo/email/phone/address visibility, date format, and page limit.
4. Update or delete a CVPresentation (`UpdateCVPresentationUseCase`/`DeleteCVPresentationUseCase`) — delete is a plain single-aggregate delete; it has no cross-aggregate cleanup to perform.
5. Fetch a CVPresentation (`GetCVPresentationUseCase`) for editing or preview.

---

## 4. Export a CVPresentation

**Goal:** Produce a downloadable PDF from a curated CVPresentation.

1. Trigger export (`ExportCVPresentationUseCase`, `GET /api/cv-presentations/{id}/export`).
2. The use case resolves every selection in order against the owner's ProfessionalProfile, applies visibility flags (`IncludeEmail`/`IncludePhone`/`IncludeAddress`), formats `YearMonth` dates locale-aware, and rejects explicitly rather than silently ignoring: an unsupported `TemplateKey` (`UnsupportedTemplate` — only `"modern-one-page"` renders), `IncludePhoto=true` (`UnsupportedPhoto` — no photo upload/storage path exists), or a rendered page count over `PageLimit` (`PageLimitExceeded`).
3. `QuestPdfCVExportRenderer` renders the one supported A4 template, including restricted-Markdown content (`RestrictedMarkdownParser` — no images, no raw HTML, links limited to https/http/mailto).
4. The frontend triggers a Blob download on success, or shows an inline message for `PresentationNotFound`/`PageLimitExceeded`/`UnsupportedTemplate`/`UnsupportedPhoto`.

---

## 5. E2E Journeys (Playwright)

These two flows are validated end-to-end when explicitly invoked (never automatically, never part of ordinary PR validation):

1. **Authenticated access** (`001-authenticated-access.spec.ts`) — open the app, authenticate via magic link (test auth scheme), land on the app shell, confirm `GET /api/me` is authorized, and that logout ends the session.
2. **CVPresentation edit + export** (`004-cv-presentation-export.spec.ts`) — seed a ProfessionalProfile with an Experience entry via API, create a CVPresentation, select and order canonical entries, set format rules, and export — verified via a real Playwright `download` event (`%PDF-` magic bytes); the parsed-output half (required text, exclusions, ordering, locale, page limit) is proven separately at Layers 1–4 via PdfPig.
