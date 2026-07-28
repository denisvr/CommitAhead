# CommitAhead — Use Cases

Key user journeys. Each maps to one or more application use case classes in the `Features/` folder structure. Every journey below operates entirely within the authenticated request's own data (ADR-0015) — StudyItems, the ProfessionalProfile, evidence sources, and drafts referenced anywhere in this document belong to that one user; there is no journey that reads or writes another user's data.

---

## 1. Daily Preparation

**Goal:** Open the app and know what to study next.

1. Load the ranked study queue (`GetRankedStudyQueue`) — filtered to Active items, ordered by EffectiveScore descending.
2. Select a StudyItem from the top of the queue.
3. Study the item (read notes, attempt the problem, review the STAR story).
4. Submit a StudyReview (`SubmitStudyReview`) with a confidence rating (1–5). Mastery updates on the next queue load.

---

## 2. Create and Manage StudyItems

**Goal:** Add a new topic to the study queue.

1. Choose a category (Theory, LeetCode, SystemDesign, Behavioral).
2. Fill in core fields (title, importance, initialMastery, tags).
3. Fill in category-specific details (typed form per category).
4. Save (`CreateStudyItem`). Item appears in the Active queue.

**Archival:** Mark an item as Archived (`ArchiveStudyItem`). It disappears from the queue; history is preserved.

**Deletion:** Delete an item (`DeleteStudyItem`) — only permitted when no StudyReviews exist and no EvidenceLinks target it. Otherwise, archival is the only option.

---

## 3. Analyse a Job Posting

**Goal:** Extract requirements, identify gaps, and link relevant StudyItems.

1. Create a JobAnalysis with a title and a JobSource (paste text or upload PDF) (`CreateJobAnalysis`).
2. For PDF uploads: text is extracted immediately; the user sees the extracted text for verification.
3. Trigger `AnalyzeJobAnalysis` (explicit user action). AI receives: job posting text + minimal profile skills summary + compact StudyItem catalogue.
4. Review the AnalysisDraft; choices remain editable in the UI until Apply:
   - **StructuredSuggestions** — accept to add JobRequirements or JobGaps via domain commands; reject to discard.
   - **LinkProposals** — accept to create EvidenceLinks from this JobAnalysis to existing StudyItems.
   - **StudyItemProposals** — accept to create new StudyItems not yet in the queue.
5. Apply the draft (`ApplyAnalysisDraft`) with exactly one Accepted/Rejected decision per proposal and a complete final payload for every accepted actionable proposal. Original AI payloads remain immutable; accepted payloads, effects, final statuses, and the draft transition execute atomically.

---

## 4. Record an Interview

**Goal:** Capture what happened in a real interview and link it to preparation.

1. Create an InterviewNote (`CreateInterviewNote`) with: company, role, round, sequence number, date, questions asked, gaps observed, lessons learned.
2. Optionally link to a JobAnalysis.
3. Optionally trigger `AnalyzeInterviewNote`. AI receives the structured note plus the compact StudyItem catalogue to propose EvidenceLinks with valid IDs and identify missing StudyItems.
4. Review and apply the AnalysisDraft.

---

## 5. Manage the Professional Profile

**Goal:** Keep the canonical CV data up to date.

1. Edit ProfessionalProfile sections: add/edit/remove ExperienceEntry, EducationEntry, Skill, LanguageEntry, CertificationEntry, ProjectEntry, ProfileLink.
2. Manage independent CVPresentation aggregates: create a presentation for a target market (`CreateCVPresentation`), select and order canonical entries, set format rules (locale, template, photo, personal-details visibility, date format, page limit, summary override).
3. Trigger `AnalyzeCVPresentation`. The application resolves the selected canonical entries into a minimised presentation projection and includes the compact StudyItem catalogue; contact values, photo keys, reviews, private notes, and hidden solutions are excluded.
4. Review and apply the AnalysisDraft.
5. Export / preview the CVPresentation (`ExportCVPresentation`).

---

## 6. Adjust Priority

**Goal:** Manually override a StudyItem's computed ranking.

1. Set a PriorityOverride on a StudyItem (`SetPriorityOverride`) with a score (0–100) and a required reason.
2. The item is ranked by the override score instead of the computed EffectiveScore.
3. Clear the override (`ClearPriorityOverride`) to restore computed ranking.

---

## 7. Configure Scoring Weights

**Goal:** Adjust how Importance, Demand, and Mastery gap contribute to EffectiveScore.

1. Open scoring configuration.
2. Edit weights (`UpdateScoringConfig`). Validation: all non-negative, sum = 100.
3. Save. The ranked-list query uses the new weights on the next load.
4. Reset to defaults (`ResetScoringConfig`) — removes the override row; code defaults (40/35/25) apply.

---

## 8. E2E Journeys (Playwright)

These four flows are validated end-to-end after every merge to main:

1. **Authenticated access** — open the app, authenticate via magic link (test auth scheme), land on the study queue.
2. **Create → Review → Rank** — create a StudyItem, submit a StudyReview, verify it appears in the correct position in the ranked queue.
3. **Job analysis draft flow** — create a JobAnalysis, trigger analysis (FakeAIProvider), review proposals, accept some, reject others, apply, verify EvidenceLinks created and draft status = Applied.
4. **CVPresentation edit + export** — select canonical entries, set format rules, save, export, verify exported content includes required fields and respects locale formatting.
