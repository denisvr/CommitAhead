# AI commands produce AnalysisDrafts; AI never writes to domain entities directly

The three AI commands (AnalyzeCVPresentation, AnalyzeJobAnalysis, AnalyzeInterviewNote) do not modify any domain entity. Each command produces an `AnalysisDraft` containing typed proposals — SuggestionProposals, LinkProposals, StudyItemProposals — which the user reviews and accepts or rejects individually. Only accepted proposals fan out to domain writes, atomically, via normal domain commands.

This constraint exists because AI output is untrusted: it may propose invalid IDs, malformed fields, or content shaped by prompt injection in the source material. Interposing a human-confirmed draft makes AI concerns structurally separate from domain writes, enforces schema validation before any state change, and makes the "AI as assistant, not actor" principle impossible to accidentally violate.
