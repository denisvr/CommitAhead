---
status: accepted
date: 2026-07-28
---

# Unified StudyItem with typed category details

## Context

CommitAhead covers four preparation categories: Theory, LeetCode, SystemDesign, and Behavioral. Each category has meaningfully different fields (e.g. LeetCode needs problem number and complexity; Behavioral needs STAR fields). The design question was whether to model them as separate entities or as a single unified type.

## Decision

A single `StudyItem` entity is used for all four categories. Category-specific structure is captured in a typed `details` value object — a discriminated union of `LeetCodeDetails`, `SystemDesignDetails`, `BehavioralDetails`, and `TheoryDetails`. The `StudyItem` title is the canonical name; details variants never duplicate it.

## Consequences

- The study queue is a single ranked list — cross-category prioritisation ("should I do LeetCode or Behavioral today?") is trivial.
- Shared fields (importance, mastery, demand, tags, status) are defined once and behave identically across all categories.
- The typed details union preserves category-specific validation, filtering, and UI structure without inheritance.
- **Implementation note (Phase 1):** the discriminated union is persisted as a single `jsonb` column (`study_items.details`) with a self-describing `kind` tag, mapped by an Infrastructure-only `StudyItemDetailsJsonConverter`/EF `ValueConverter` pair — the Domain layer has no serialization awareness. See `docs/architecture/persistence.md` ("Typed category details") for the full mapping strategy.

## Considered Alternatives

Four separate entities (LeetCodeItem, SystemDesignItem, etc.) would require a cross-entity ranking layer to produce a unified queue, adding complexity without domain benefit. A single flat entity with nullable category-specific columns was rejected because it offers no compile-time type safety and produces a sparse table.
