import type { components } from '../../api/generated/schema'

export type StructuredSuggestionCommandType = NonNullable<components['schemas']['StructuredSuggestionCommandType']>
export type StudyItemCategory = components['schemas']['StudyItemCategory']

export type SuggestionFields = Record<string, string>

/**
 * Parses one command's ProposedPayloadJson into flat, editable string fields. AddJobRequirement/
 * AddJobGap's proposed shape carries an already-assigned Guid the accepted decision must NOT
 * repeat (backend/ApplyAnalysisDraftUseCase's own decision-only payload records omit it) — dropped
 * here rather than surfaced as an editable field.
 */
export function parseSuggestionFields(commandType: StructuredSuggestionCommandType, proposedPayloadJson: string): SuggestionFields {
  const parsed = JSON.parse(proposedPayloadJson) as Record<string, unknown>
  switch (commandType) {
    case 'AddJobRequirement':
      return { text: String(parsed.Text ?? ''), kind: String(parsed.Kind ?? ''), priority: String(parsed.Priority ?? ''), sourceExcerpt: String(parsed.SourceExcerpt ?? '') }
    case 'AddJobGap':
      return { matchLevel: String(parsed.MatchLevel ?? ''), severity: String(parsed.Severity ?? ''), rationale: String(parsed.Rationale ?? '') }
    case 'UpdateCVPresentationSummary':
      return { summaryMarkdown: parsed.SummaryMarkdown == null ? '' : String(parsed.SummaryMarkdown) }
    case 'AddInterviewGap':
    case 'AddInterviewLesson':
      return { text: String(parsed.Text ?? '') }
  }
}

/** Inverse of parseSuggestionFields — builds the AcceptedPayloadJson the backend's decision-shape expects for this command. */
export function buildSuggestionPayloadJson(commandType: StructuredSuggestionCommandType, fields: SuggestionFields): string {
  switch (commandType) {
    case 'AddJobRequirement':
      return JSON.stringify({ Text: fields.text, Kind: fields.kind, Priority: fields.priority, SourceExcerpt: fields.sourceExcerpt })
    case 'AddJobGap':
      return JSON.stringify({ MatchLevel: fields.matchLevel, Severity: fields.severity, Rationale: fields.rationale })
    case 'UpdateCVPresentationSummary':
      return JSON.stringify({ SummaryMarkdown: fields.summaryMarkdown || null })
    case 'AddInterviewGap':
    case 'AddInterviewLesson':
      return JSON.stringify({ Text: fields.text })
  }
}

export const JOB_REQUIREMENT_KINDS = ['Technical', 'Behavioural', 'Experience', 'Domain', 'Language', 'Other']
export const JOB_REQUIREMENT_PRIORITIES = ['Required', 'Preferred']
export const JOB_GAP_MATCH_LEVELS = ['Partial', 'Missing', 'Unknown']
export const JOB_GAP_SEVERITIES = ['High', 'Medium', 'Low']
export const LEETCODE_DIFFICULTIES = ['Easy', 'Medium', 'Hard']

export type StudyItemDetailsFields = Record<string, string>

const STRING_ARRAY_KEYS: Record<StudyItemCategory, string[]> = {
  Theory: ['KeyPoints', 'InterviewQuestions', 'References'],
  LeetCode: ['Patterns'],
  SystemDesign: ['ClarifyingQuestions', 'FunctionalRequirements', 'NonFunctionalRequirements', 'EvaluationChecklist'],
  Behavioral: ['Competencies', 'QuestionVariants'],
}

/** Multiline-textarea-friendly round trip for the array-of-string fields each category's details carry. */
export function joinLines(values: string[]): string {
  return values.join('\n')
}

export function splitLines(text: string): string[] {
  return text
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
}

export function parseStudyItemDetailsFields(category: StudyItemCategory, detailsJson: string): StudyItemDetailsFields {
  const parsed = JSON.parse(detailsJson) as Record<string, unknown>
  const fields: StudyItemDetailsFields = {}
  for (const [key, value] of Object.entries(parsed)) {
    if (STRING_ARRAY_KEYS[category].includes(key)) {
      fields[key] = joinLines((value as string[] | null) ?? [])
    } else {
      fields[key] = value == null ? '' : String(value)
    }
  }

  return fields
}

export type StudyItemDetailFieldSpec = { key: string; label: string; input: 'text' | 'textarea' | 'multiline' | 'select'; options?: string[] }

export const STUDY_ITEM_DETAIL_FIELD_SPECS: Record<StudyItemCategory, StudyItemDetailFieldSpec[]> = {
  Theory: [
    { key: 'SummaryMarkdown', label: 'Summary', input: 'textarea' },
    { key: 'KeyPoints', label: 'Key points (one per line)', input: 'multiline' },
    { key: 'InterviewQuestions', label: 'Interview questions (one per line)', input: 'multiline' },
    { key: 'References', label: 'References (one URL per line)', input: 'multiline' },
  ],
  LeetCode: [
    { key: 'ProblemNumber', label: 'Problem number', input: 'text' },
    { key: 'Url', label: 'URL', input: 'text' },
    { key: 'Difficulty', label: 'Difficulty', input: 'select', options: LEETCODE_DIFFICULTIES },
    { key: 'Patterns', label: 'Patterns (one per line)', input: 'multiline' },
    { key: 'ExpectedTimeComplexity', label: 'Expected time complexity', input: 'text' },
    { key: 'ExpectedSpaceComplexity', label: 'Expected space complexity', input: 'text' },
    { key: 'ApproachMarkdown', label: 'Approach', input: 'textarea' },
    { key: 'CSharpSolution', label: 'C# solution (optional)', input: 'textarea' },
  ],
  SystemDesign: [
    { key: 'PromptMarkdown', label: 'Prompt', input: 'textarea' },
    { key: 'ClarifyingQuestions', label: 'Clarifying questions (one per line)', input: 'multiline' },
    { key: 'FunctionalRequirements', label: 'Functional requirements (one per line)', input: 'multiline' },
    { key: 'NonFunctionalRequirements', label: 'Non-functional requirements (one per line)', input: 'multiline' },
    { key: 'EvaluationChecklist', label: 'Evaluation checklist (one per line)', input: 'multiline' },
    { key: 'ReferenceSolutionMarkdown', label: 'Reference solution', input: 'textarea' },
  ],
  Behavioral: [
    { key: 'Competencies', label: 'Competencies (one per line)', input: 'multiline' },
    { key: 'QuestionVariants', label: 'Question variants (one per line)', input: 'multiline' },
    { key: 'Situation', label: 'Situation', input: 'textarea' },
    { key: 'Task', label: 'Task', input: 'textarea' },
    { key: 'Action', label: 'Action', input: 'textarea' },
    { key: 'Result', label: 'Result', input: 'textarea' },
    { key: 'Reflection', label: 'Reflection (optional)', input: 'textarea' },
  ],
}

export function buildStudyItemDetailsJson(category: StudyItemCategory, fields: StudyItemDetailsFields): string {
  const result: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(fields)) {
    result[key] = STRING_ARRAY_KEYS[category].includes(key) ? splitLines(value) : value
  }

  if (category === 'LeetCode') {
    result.ProblemNumber = fields.ProblemNumber ? Number(fields.ProblemNumber) : null
    result.Url = fields.Url || null
    result.CSharpSolution = fields.CSharpSolution || null
  }

  return JSON.stringify(result)
}
