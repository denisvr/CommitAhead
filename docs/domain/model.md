# CommitAhead — Domain Model

For term definitions see `CONTEXT.md`. For architectural decisions see `docs/adr/`.

---

## Aggregate Roots

Every aggregate root below carries an `ownerUserId` (see ADR-0015): all reads, writes, and cross-aggregate references are scoped to the authenticated request's owner. There is no cross-user sharing — a user's data is invisible to every other user, full stop.

### 1. ProfessionalProfile *(singleton per user)*

The canonical source of professional identity and reusable CV content — one per user, not a single global row. A user's first save creates their row; there is never more than one per `ownerUserId`.

| Field | Type |
|---|---|
| `id` | UUID |
| `ownerUserId` | UUID |
| `contactInfo` | ContactInfo |
| `summaryMarkdown` | string |
| `createdAt` | timestamp |
| `updatedAt` | timestamp |

**Child entity collections:**

#### ExperienceEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `company` | string |
| `client` | string? |
| `role` | string |
| `employmentType` | Permanent \| Contract \| Freelance \| Internship \| Other |
| `startDate` | YearMonth |
| `endDate` | YearMonth? (`null` = current) |
| `location` | string? |
| `workMode` | OnSite \| Hybrid \| Remote \| Other |
| `summaryMarkdown` | string |
| `achievements` | string[] |
| `skillIds` | UUID[] referencing Skills in the same profile |

#### EducationEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `institution` | string |
| `degree` | string |
| `field` | string? |
| `startDate` | YearMonth? |
| `endDate` | YearMonth? |
| `location` | string? |
| `detailsMarkdown` | string? |

#### Skill

| Field | Type |
|---|---|
| `id` | UUID |
| `displayName` | string |
| `normalizedKey` | string | Trimmed, lowercase, kebab-case; unique inside the profile |
| `category` | SkillCategory |
| `proficiency` | Beginner \| Intermediate \| Advanced \| Expert \| null |

`SkillCategory`: Language, Framework, Platform, Cloud, Database, Messaging, DevOps, Testing, Architecture, Tool, Methodology, Domain, Other.

#### LanguageEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `language` | string |
| `proficiency` | A1 \| A2 \| B1 \| B2 \| C1 \| C2 \| Native |
| `certification` | string? |

#### CertificationEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `name` | string |
| `issuingOrganisation` | string |
| `issuedAt` | YearMonth? |
| `expiresAt` | YearMonth? |
| `credentialId` | string? |
| `url` | absolute http/https URL? |

#### ProjectEntry

| Field | Type |
|---|---|
| `id` | UUID |
| `name` | string |
| `role` | string? |
| `startDate` | YearMonth? |
| `endDate` | YearMonth? |
| `descriptionMarkdown` | string |
| `url` | absolute http/https URL? |
| `skillIds` | UUID[] referencing Skills in the same profile |

#### ProfileLink

| Field | Type |
|---|---|
| `id` | UUID |
| `kind` | GitHub \| LinkedIn \| Portfolio \| Blog \| Other |
| `label` | string? |
| `url` | absolute http/https URL |

CVPresentations are not children of ProfessionalProfile. They are an independent aggregate root because they have their own lifecycle and export use case. See ADR-0012.

---

### 2. CVPresentation

A curated, locale-specific projection over one ProfessionalProfile.

| Field | Type | Notes |
|---|---|---|
| `id` | UUID | |
| `ownerUserId` | UUID | FK to the owning User (ADR-0015) |
| `professionalProfileId` | UUID | FK to the user's own ProfessionalProfile — must share `ownerUserId` |
| `label` | string | e.g. “UK — Senior Backend Engineer” |
| `targetMarket` | string | Country/market identifier |
| `targetRole` | string? | |
| `locale` | string | BCP 47 locale |
| `templateKey` | string | References an available export template |
| `summaryOverrideMarkdown` | string? | Null uses profile summary |
| `includePhoto` | bool | |
| `includeEmail` | bool | |
| `includePhone` | bool | |
| `includeAddress` | bool | |
| `dateFormat` | string | Locale-aware rendering rule |
| `pageLimit` | int > 0 | |
| `createdAt` | timestamp | UTC |
| `updatedAt` | timestamp | UTC |

It owns seven ordered selection collections: Experience, Education, Skill, Language, Certification, Project, and ProfileLink. Each selection is an ordered list of canonical entry IDs — list order *is* position (invariant 5); there is no separate stored `position` value alongside each ID (see ADR-0017 for the persistence shape). ProfileLinks default to all existing links when a presentation is created but can be explicitly excluded afterward.

---

## Value Objects

### ContactInfo

```text
name: string
email: string
phone: string?
address: string?
photoStorageKey: string?
```

### YearMonth

```text
year: int
month: int [1, 12]
```

---

## Domain Invariants

1. ProfessionalProfile Skill.normalizedKey is unique.
2. Experience and Project skill IDs must exist in their owning profile.
3. A Skill referenced by Experience or Project entries cannot be deleted until those references are removed or reassigned.
4. CVPresentation selection IDs must exist in its referenced ProfessionalProfile.
5. Every CVPresentation selection collection has unique entry IDs and unique, contiguous positions starting at zero.
6. Deleting a canonical profile entry removes its ID from any CVPresentation's ordered `uuid[]` selection array but never deletes a CVPresentation.
7. Every cross-aggregate reference connects entities owned by the same user (see ADR-0015): a CVPresentation's `professionalProfileId` must share the referencing entity's `ownerUserId`. There is no cross-user reference, ever.

---

## Cross-Aggregate References

All cross-aggregate references are IDs only; domain navigation properties never cross aggregate boundaries. Every reference below is additionally scoped to the same `ownerUserId` on both ends (invariant 7) — a user can only ever reference their own data.

| From | Field | References |
|---|---|---|
| CVPresentation | `professionalProfileId` | ProfessionalProfile |
| CVPresentation selections | `entryId` | Canonical child entries owned by its ProfessionalProfile |

`CVPresentation.professionalProfileId` is a real foreign key (a composite `(ProfessionalProfileId, OwnerUserId)` FK against a matching alternate key on `ProfessionalProfile` — see ADR-0017), enforced by the database as well as the application layer. The CVPresentation selection collections reference canonical child entries by plain `uuid[]` columns, not FK-backed join tables (ADR-0017) — the domain aggregate enforces referential integrity for those in memory.
