# CommitAhead

A private, single-user web application for structured interview preparation. It maintains a ranked study queue, a professional profile, and an evidence layer (job analyses, interview notes) that together drive what to study next.

## Language

### Study Queue

**StudyItem**: The primary unit of preparation. Belongs to one category (Theory, LeetCode, SystemDesign, Behavioral), carries typed category details, normalised tags, an Importance rating, and an InitialMastery estimate. The study queue is the ranked list of active StudyItems.
_Avoid_: Topic, card, item, task

**StudyReview**: A record of one session spent on a StudyItem, capturing the date and a confidence rating (1–5). The aggregate of recent reviews determines the item's current Mastery.
_Avoid_: Practice record, review session

**EffectiveScore**: The computed 0–100 priority value used to rank StudyItems. Derived from Importance (40%), Demand (35%), and Mastery gap (25%), or replaced entirely by a PriorityOverride.
_Avoid_: Priority, rank, score

**PriorityOverride**: A user-set score (0–100) and required reason that replaces the computed EffectiveScore for a StudyItem. Its presence implies the override is active; clearing it restores computed ranking.
_Avoid_: Manual priority, pinned score

**Importance**: A manual 1–5 rating of how strategically significant a StudyItem is to the user's preparation goals. Set by the user; never computed.
_Avoid_: Weight, relevance

**Mastery**: A 1–5 value representing the user's current command of a StudyItem. Equals InitialMastery until the first StudyReview exists; thereafter the average of the three most recent confidence ratings.
_Avoid_: Skill level, proficiency, confidence

**InitialMastery**: A self-assessed 1–5 mastery value set at creation, used in EffectiveScore until the first StudyReview is recorded.
_Avoid_: Default mastery, starting mastery

**Demand**: A 0–5 value representing how urgently confirmed EvidenceLinks signal that a StudyItem is needed. Computed as `min(sum of confirmed EvidenceLink weights pointing to this item, 5)`.
_Avoid_: Relevance, urgency, market demand

**ScoringConfig**: The three weight percentages (Importance, Demand, Mastery gap) that govern the EffectiveScore formula. Code-level defaults are 40/35/25; a single optional database row holds user overrides. Weights must be non-negative and sum to 100.
_Avoid_: Score settings, ranking config

### Category Details

**LeetCodeDetails**: Typed details for a StudyItem of category LeetCode. Carries problem number (optional), URL, difficulty, patterns, expected time and space complexity, approach notes (Markdown), and an optional C# solution.

**SystemDesignDetails**: Typed details for a StudyItem of category SystemDesign. Carries a design prompt, clarifying questions, functional and non-functional requirements, an evaluation checklist, and a reference solution (Markdown). The reference solution is hidden in the UI until explicitly revealed; this is transient UI state only.

**BehavioralDetails**: Typed details for a StudyItem of category Behavioral. Carries competencies, question variants, and a STAR breakdown (situation, task, action, result, optional reflection).

**TheoryDetails**: Typed details for a StudyItem of category Theory. Carries a summary (Markdown), key points, interview questions, and references.

### Professional Profile

**ProfessionalProfile**: The singleton canonical record of the user's professional identity. Contains six ordered canonical collections — ExperienceEntry, EducationEntry, Skill, LanguageEntry, CertificationEntry, ProjectEntry — plus ProfileLinks. CVPresentations are separate aggregate roots that reference and curate these collections; entries are never duplicated.
_Avoid_: CV, resume, profile

**CVPresentation**: An independently addressable aggregate root representing a curated, locale-specific view over one ProfessionalProfile. Selects and orders entries from each canonical collection by ID, may override the summary, and carries formatting rules: targetMarket, locale, template, photo inclusion, personal-details visibility, date format, and page limit.
_Avoid_: CV version, regional CV, tailored resume

**ContactInfo**: Global identity and contact data held on the ProfessionalProfile — name, email, phone, address, and optional photo (Supabase Storage key). Always belongs to the profile; never duplicated on a CVPresentation. Per-presentation visibility rules (`includePhoto`, `includeEmail`, `includePhone`, `includeAddress`) control what is rendered.

**ExperienceEntry**: A canonical employment record inside a ProfessionalProfile. Carries company, optional client, role, employment type, dates, location, work mode, a summary (Markdown), achievements, and references to canonical Skill IDs.
_Avoid_: Job entry, work history item

**EducationEntry**: A canonical academic record inside a ProfessionalProfile. Carries institution, degree, optional field of study, optional dates, optional location, and optional details (Markdown).
_Avoid_: Degree, academic record

**Skill**: A canonical skill inside a ProfessionalProfile. Carries a display name, a normalised key (lowercase kebab-case), a category (Language, Framework, Platform, Cloud, Database, Messaging, DevOps, Testing, Architecture, Tool, Methodology, Domain, Other), and an optional proficiency level.
_Avoid_: Technology, competency, tool

**LanguageEntry**: A canonical spoken-language record inside a ProfessionalProfile. Carries the language name, a CEFR proficiency level (A1–C2) or Native, and an optional certification (e.g. "IELTS 8.0").
_Avoid_: Spoken language, language skill

**CertificationEntry**: A canonical professional certification inside a ProfessionalProfile. Carries name, issuing organisation, optional issued and expiry dates, optional credential ID, and optional URL.
_Avoid_: Certificate, credential

**ProjectEntry**: A canonical project record inside a ProfessionalProfile. Carries name, optional role, optional dates, a description (Markdown), optional URL, and references to canonical Skill IDs.
_Avoid_: Side project, portfolio item

**ProfileLink**: A canonical online presence link inside a ProfessionalProfile. Carries a kind (GitHub, LinkedIn, Portfolio, Blog, Other), an optional label, and a URL. CVPresentations select which ProfileLinks to include; all are included by default.
_Avoid_: Social link, online profile

### Evidence Sources

**JobAnalysis**: A record created for a specific job posting. Holds a JobSource plus extracted JobRequirements and JobGaps. No application pipeline status is tracked.
_Avoid_: Job posting, job record, job application

**JobSource**: The raw input of a JobAnalysis. Either a PastedText (inline string) or an UploadedFile (Storage key, original filename, MIME type, and pre-extracted text). AI always receives the extracted text; the distinction is preserved for provenance.
_Avoid_: Job text, source file

**JobRequirement**: A single requirement extracted from a job posting. Carries text, kind (Technical, Behavioural, Experience, Domain, Language, Other), priority (Required or Preferred), and the source excerpt that supports it.
_Avoid_: Required skill, job criterion

**JobGap**: A gap between a JobRequirement and the user's ProfessionalProfile. References its parent JobRequirement and carries a match level (Partial, Missing, Unknown), severity (High, Medium, Low), and rationale. Only created when the requirement is not fully matched.
_Avoid_: Skill gap, missing skill

**InterviewNote**: A structured record of a real interview. Carries date, company, role, InterviewRound, sequence number, questions asked, gaps observed, and lessons learned. Optionally linked to a JobAnalysis.
_Avoid_: Interview record, debrief note

**InterviewRound**: The stage of an interview process — RecruiterScreening, HiringManager, Technical, LiveCoding, TakeHome, SystemDesign, Behavioral, Panel, Final, or Other. When Other, a label is required.
_Avoid_: Interview stage, round type

### AI Analysis

**EvidenceLink**: A confirmed connection from an evidence source (CVPresentation, JobAnalysis, or InterviewNote) to a StudyItem, carrying a weight (0–5) and a rationale. Created only from an accepted LinkProposal. Existence means the link is active; there is no separate lifecycle flag. At most one EvidenceLink may exist per source–StudyItem pair.
_Avoid_: Tag link, relevance link, demand link

**AnalysisDraft**: The output of one AI analysis command. Contains three typed proposal collections: SuggestionProposals, LinkProposals, and StudyItemProposals. Each proposal carries its own Pending/Accepted/Rejected status. Draft status transitions Pending → Applied or Pending → Discarded. Only one Pending draft may exist per evidence source at a time.
_Avoid_: AI result, analysis output

**SuggestionProposal**: A proposal within an AnalysisDraft to change the analysed source. Either a StructuredSuggestion (typed command payload; applying fires a domain command) or an AdvisorySuggestion (free-form text; applying marks it Accepted for manual follow-up). AI never edits a source directly.
_Avoid_: AI recommendation, improvement suggestion

**LinkProposal**: A proposal within an AnalysisDraft to create an EvidenceLink from the analysed source to an existing StudyItem. Carries proposed weight and rationale. Accepted proposals become confirmed EvidenceLinks.
_Avoid_: Demand proposal, link suggestion

**StudyItemProposal**: A proposal within an AnalysisDraft to create a new StudyItem identified as absent from the study queue. Carries proposed title, category, typed details, tags, and importance. Because AI cannot know the user's Mastery, accepting it requires the user to supply InitialMastery in the final proposal decision.
_Avoid_: New topic proposal, suggested item

**IAIProvider**: The backend abstraction over the AI provider. The only boundary at which real AI calls occur. Receives text and structured output instructions; never called from the frontend or domain layer.
_Avoid_: AI service, LLM client

**AIUsageRecord**: An operational record reserved before an AI call and reconciled afterward. Carries a unique idempotency key, command/source identity, provider/model/pricing version/currency, Reserved/Completed/Failed status, reserved token/cost limits, actual provider-reported usage, optional AnalysisDraft ID, timestamps, and a safe outcome code. Never stores prompt or response content.
_Avoid_: AI log, cost record
