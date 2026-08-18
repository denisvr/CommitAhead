# CommitAhead

A private, invite-only web application for maintaining one canonical professional profile and curating it into locale-specific, exportable CV presentations, with every user's data isolated by owner (see ADR-0015).

## Language

### Professional Profile

**ProfessionalProfile**: The canonical record of a user's professional identity — a singleton per user (ADR-0015), not a single global record. Contains six ordered canonical collections — ExperienceEntry, EducationEntry, Skill, LanguageEntry, CertificationEntry, ProjectEntry — plus ProfileLinks. CVPresentations are separate aggregate roots that reference and curate these collections; entries are never duplicated.
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
