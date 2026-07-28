---
status: accepted
date: 2026-07-28
---

# API is the composition root and may register Infrastructure

## Context

Controllers must depend only on Application use cases. However, the running process still needs to construct Infrastructure implementations for repositories, PostgreSQL, Storage, PDF extraction, Data Protection, and the configured AI adapter.

Completely prohibiting the API assembly from referencing Infrastructure would require a fifth bootstrapper project or runtime assembly discovery solely to register dependencies.

## Decision

`CommitAhead.Api` is the composition root. Its startup code may reference Infrastructure registration extensions, such as `services.AddInfrastructure(configuration)`.

Controllers, filters containing business orchestration, and API contracts may not depend on Infrastructure types, repositories, or DbContext. The exception is limited to `Program.cs` and dedicated dependency-registration code.

## Consequences

- The four-project backend remains sufficient; no bootstrapper assembly is introduced.
- Infrastructure dependencies remain explicit at process startup.
- NetArchTest enforces controller-level isolation rather than an impossible assembly-wide API → Infrastructure prohibition.

## Considered Alternatives

A separate Bootstrapper project would preserve a stricter assembly graph but adds a project with no domain value. Reflection-based registration without a project reference hides dependencies and weakens build-time verification. Both were rejected for the MVP.
