# Zero real AI calls in automated CI; live smoke tests are manual-only

The PR pipeline makes no real AI calls under any circumstances. All AI command paths are exercised via `FakeAIProvider`, a deterministic handwritten implementation of `IAIProvider` that returns scenario-driven fixture responses (success, empty output, malformed proposals, duplicates, timeout, provider failure).

Real provider smoke tests exist but run only when manually triggered via a dedicated workflow requiring explicit parameters: provider, model, maximum input tokens, maximum output tokens, and cost ceiling. They assert schema validity and deserialisation correctness only — never exact AI wording.

The reason is twofold: cost and determinism. An automated pipeline that calls a real provider incurs unbounded cost on every push, makes tests non-deterministic (AI output varies), and risks exposing provider keys in CI logs. The `FakeAIProvider` contract is sufficient to prove application behaviour; real provider behaviour is validated by the adapter's own unit tests with stubbed HTTP responses.
