---
status: accepted
date: 2026-07-28
---

# Zero real AI calls in automated CI; live smoke tests are manual-only

**Status: superseded — this feature was removed from the app (see docs/roadmap.md). Kept for historical record.**

## Context

Three AI commands are core MVP features. They must be testable in CI without incurring real provider costs or introducing non-determinism. At the same time, the real provider adapter must be verified to deserialise and validate responses correctly.

## Decision

The PR pipeline makes zero real AI calls under any circumstances. All AI command paths are exercised via `FakeAIProvider`, a deterministic handwritten `IAIProvider` implementation with six scenario-driven fixture responses per command: success, empty output, malformed proposals, duplicates, timeout, and provider failure.

The real provider adapter (`ProviderAIAdapter`, renamed when the provider is selected) is tested separately with stubbed HTTP/SDK responses that exercise deserialisation, request construction, token-limit enforcement, and error mapping — without making network calls.

Live provider smoke tests exist but are triggered only via a dedicated manual workflow requiring explicit parameters: provider, model, maximum input tokens, maximum output tokens, and cost ceiling. They assert schema validity and deserialisation correctness only — never exact AI wording. They are never scheduled and never run automatically.

## Consequences

- The CI pipeline is fully deterministic and costs nothing in AI usage.
- `FakeAIProvider` must be kept consistent with the `IAIProvider` contract. Any change to the interface requires updating the fake.
- The six fixture scenarios are sufficient to prove application behaviour across all AI outcomes; real provider quality (output relevance, instruction following) is validated by the manual smoke tests.

## Considered Alternatives

Mocking `IAIProvider` with NSubstitute per test was considered but rejected in favour of a single, scenario-driven `FakeAIProvider`. Per-test mocks spread fixture data across many test files and make it easy to accidentally test against an unrealistic response shape. A shared fake with named scenarios is more maintainable and easier to audit.
