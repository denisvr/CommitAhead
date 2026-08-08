---
status: accepted
date: 2026-08-08
---

# The AI provider is Anthropic, model Claude Haiku 4.5

## Context

`docs/tbd.md`'s "AI provider selection" entry blocked `ProviderAIAdapter` (Infrastructure) — the
only implementation of `IAIProvider` that ever makes a real network call (ADR-0009: `FakeAIProvider`
is the only implementation used in tests/CI). The candidates were Anthropic, OpenAI, Azure OpenAI,
and Google (Vertex AI Gemini), each evaluated against the constraints already fixed by earlier
phases: EU-compliant privacy terms, training on submitted data opt-out or disabled, minimal data
retention where supported, and reliable structured output (JSON schema enforcement) — the three
`AnalyzeX` use cases depend on the provider returning a validated `StructuredSuggestion`/
`LinkProposal`/`StudyItemProposal` shape, not free text to be parsed with regex.

A separate question — which model tier — was resolved alongside it: every `AnalyzeX` command is
extraction-plus-suggestion over already-narrow, pre-minimised input (`JobAnalysisAiInput`/
`CVPresentationAiInput`/`InterviewNoteAiInput`), not open-ended reasoning, and every output still
requires human confirmation before anything is applied (ADR-0005) — a wrong or shallow suggestion
from the model has no direct effect, it is just a proposal the user can reject.

## Decision

**Provider:** Anthropic, called directly (not via a third-party gateway). **Model:** Claude Haiku
4.5, for all three `AnalyzeX` commands — there is no per-command model override in this decision;
`AiProviderDescriptor.Model` returning a different value per `AiCommandType` remains possible later
without revisiting this ADR, since `IAIProvider.Describe` is already commandType-scoped.

`ProviderAIAdapter` (Infrastructure) calls the Messages API using tool-use forced via `tool_choice`
(one tool per command, matching that command's expected structured shape) rather than free-text
completion parsed after the fact — the same "never trust the model's raw text as a database
reference" posture ADR already established at the validation layer (`AiStructuredSuggestionValidator`)
now starts one step earlier, at the call itself.

## Why

- **Meets every fixed constraint without an enterprise contract.** Anthropic's API does not train on
  submitted data by default (no opt-out step needed), publishes a Data Processing Addendum covering
  EU data transfers, and supports forced tool-use for structured output — satisfying all three
  constraints without the added account/contract surface Azure OpenAI's stronger EU data-residency
  guarantees would need for a single-operator project this size.
- **Haiku is enough for the actual task.** All three `AnalyzeX` inputs are already trimmed,
  structured, and command-scoped before the call is made; the task is closer to "extract and
  classify against a small, fixed command set" than open-ended reasoning. Pairing that with the
  cheapest capable model keeps per-call cost low without a demonstrated quality gap — and ADR-0005's
  confirmation step means a shallow suggestion costs the user a rejection, not a wrong write.
- **Matches the ecosystem this project is already built and tested in.** No new SDK family,
  authentication model, or terminology to introduce beyond what `FakeAIProvider`/ADR-0009 already
  assume in shape (a single provider abstraction called from Infrastructure only).
- **Reopening later is cheap.** `IAIProvider`/`AiProviderDescriptor` were already designed
  provider-agnostic (Slice 2) specifically so this decision would not leak into Domain or the
  `AnalyzeX` use cases — swapping the model, or overriding it per `AiCommandType`, changes only
  `ProviderAIAdapter` and its Descriptor values.

## Consequences

- `docs/tbd.md`'s "AI provider selection" entry is resolved; "Default AI budgets" now has a real
  currency/pricing basis to depend on (Anthropic's own per-token pricing, versioned into
  `AiProviderDescriptor.PricingVersion`/`Currency` once `ProviderAIAdapter` exists) but remains its
  own open decision (ceiling amounts and whether they're user-editable) — this ADR does not resolve
  that entry, only unblocks it.
- `ProviderAIAdapter` needs an Anthropic API key, held backend-only per the project's existing
  secrets posture (never exposed to the frontend, never logged) — provisioning that key for local
  dev/CI (which never calls it, ADR-0009) versus production (Phase 6, still TBD on hosting/secrets
  management) is not decided by this ADR.
- The live AI smoke test (manual-trigger only, explicit cost ceiling, never scheduled — per the
  project's CI quality gates) can now be written against a real target instead of staying purely
  hypothetical.
- If Haiku's suggestion quality proves too shallow once judged against real manual testing (not
  simulated `FakeAIProvider` fixtures), the fallback is a per-`AiCommandType` model override inside
  `ProviderAIAdapter.Describe`, not a provider change.

## Considered alternatives

- **OpenAI** — comparable structured-output support (`response_format` strict JSON Schema) and a
  similar default no-training policy, but a less direct EU privacy addendum without moving to Azure
  OpenAI specifically.
- **Azure OpenAI** — the strongest contractual EU data-residency story, but requires provisioning
  and maintaining a separate Azure account/resource purely for this, disproportionate overhead for a
  single-operator, single-user-today project (ADR-0015's "today there is exactly one real user").
- **Google (Vertex AI Gemini)** — EU regions and structured output both available, but introduces an
  entirely new SDK/authentication surface with no other use in this project, versus Anthropic and
  OpenAI which both keep the integration to a single HTTP-based API client.
- **A higher-tier model (Claude Sonnet) by default** — rejected for now on cost given the task shape
  described above; revisited only if manual testing shows Haiku's suggestions are too shallow to be
  useful, per the fallback noted above.
