---
status: accepted
date: 2026-07-28
---

# Evidence sources are not StudyItems

## Context

The system covers six preparation areas. Three of them — job analyses, interview notes, and CV presentations — are documents that inform preparation priorities. The other four (Theory, LeetCode, SystemDesign, Behavioral) are topics to actively study. An early design question was whether all six should share the same entity and queue.

## Decision

CVPresentations, JobAnalyses, and InterviewNotes are not placed in the study queue. They are evidence sources that influence what to study via EvidenceLinks, not things to study themselves.

Each AI analysis command receives only what is strictly necessary for its task:
- **AnalyzeCVPresentation** — the CVPresentation being analysed.
- **AnalyzeJobAnalysis** — the job posting text, a minimal ProfessionalProfile skills summary (required for gap detection), and a compact StudyItem catalogue (`{id, title, category, tags}`) to identify topics that may be missing from the queue.
- **AnalyzeInterviewNote** — the InterviewNote being analysed.

No command receives the full study queue, review history, notes, hidden solutions, or contact details.

## Consequences

- The score formula handles only inputs whose role is to receive priorities, not to calibrate them.
- The data minimisation boundary for AI commands is explicit and per-command, not a blanket "evidence source only" rule.
- AnalyzeJobAnalysis requires the ProfessionalProfile skills summary and StudyItem catalogue to be prepared and passed by the application layer before calling `IAIProvider`.

## Considered Alternatives

Treating evidence sources as a special StudyItem category would have conflated "what I am preparing" with "why this topic matters now", forced the ranking formula to handle unrankable inputs, and made it harder to define clean data minimisation boundaries per AI command.
