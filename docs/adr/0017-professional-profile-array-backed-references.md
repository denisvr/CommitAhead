---
status: accepted
date: 2026-08-06
---

# ProfessionalProfile/CVPresentation array-backed references and integer YearMonth

## Context

`docs/architecture/persistence.md` originally specified two things that Phase 2's implementation
diverged from:

1. FK-backed join tables for Experience/Project skill references (`experience_skills`,
   `project_skills`) and for each of CVPresentation's seven ordered selections
   (`cv_presentation_experiences`, `cv_presentation_educations`, etc., each row carrying
   `cv_presentation_id`, `entry_id`, and `position`).
2. `YearMonth` stored as two integer columns (`_year`, `_month`) on the parent row.

Both ran into the same underlying EF Core 10 limitation, discovered empirically while building the
Infrastructure slice, not decided upfront.

## Decision

- `ExperienceEntry.SkillIds`, `ProjectEntry.SkillIds`, and all seven of `CVPresentation`'s selection
  collections map as a plain `uuid[]` Postgres array column (Npgsql's native primitive-collection
  support), not a join table.
- `YearMonth` maps as a single converted `integer` column (`year * 100 + month`), not two columns
  or an owned/complex sub-object.

## Why (the actual EF Core wall)

EF Core's constructor binding requires a real collection-navigation property to materialize a
many-to-many relationship, and cannot bind a containing entity's own constructor parameter to a
nested owned/complex sub-object at all (`OwnsOne` and the EF 8+ `ComplexProperty` API both fail
identically — error: *"Navigations to related entities, including references to owned types,
cannot be bound"*).

- `SkillIds`/each selection collection are plain `IReadOnlyList<Guid>`, not navigations to `Skill`
  or canonical-entry entities — Domain deliberately keeps these as opaque IDs, validated by
  `ProfessionalProfile`/`CVPresentation` themselves rather than by loading related rows. A real
  join table would need a shadow skip-navigation plus custom materialization code to turn loaded
  entities back into a plain `Guid` list for the constructor.
- `YearMonth` hit the owned/complex-type wall directly: it is constructor-only, so mapping it as
  `OwnsOne`/`ComplexProperty` on `ExperienceEntry`/`EducationEntry`/etc. fails the same way. A
  single converted scalar column sidesteps the wall entirely (mirrors
  `ContactInfoValueConverter`'s jsonb column for the same reason).

## Trade-offs accepted

- **Lost:** a join table's DB-level FK on each individual array element — nothing at the database
  level enforces that every ID inside `skill_ids`/`*_selections` refers to a real row.
- **Kept:** every invariant the join tables would have backed up is already enforced by the domain
  aggregate itself:
  - Invariant 21 (skill references must exist) and 22 (a referenced Skill can't be deleted) — in
    `ProfessionalProfile.ReplaceSkills`/`ReplaceExperience`/`ReplaceProjects`.
  - Invariant 23 (CVPresentation selections must exist in the referenced profile) — in the seven
    `Replace*SelectionsUseCase` classes (application-enforced per ADR-0012, since it spans two
    aggregates).
  - Invariant 24 (unique, contiguous positions) is structural for both the array shape: list order
    *is* position, so there is no separate position value that could ever drift out of sync, and
    no separate per-position uniqueness constraint to maintain.
- Cross-owner reference safety (invariant 29, CVPresentation → ProfessionalProfile) is unaffected
  by this decision — it's enforced by a composite alternate key/FK between those two aggregate
  tables directly, unrelated to whether skill/selection references are arrays or join rows.

## Considered alternatives

- A real FK-backed join table with a shadow `Skill`/canonical-entry navigation, reconstructing the
  plain ID list from loaded entities after materialization. Rejected for this MVP: extra query
  cost (an additional join/`Include` per read), extra mapping code, and no invariant gap it would
  close beyond what the domain already guarantees (see "Trade-offs accepted" above).

## Status

Accepted for the MVP. Revisit only if the missing DB-level array-element FK causes a real data
integrity incident, or a query needs indexed array-containment (`entry_id = ANY(selections)`,
GIN-indexable) at a scale where that stops being adequate.
