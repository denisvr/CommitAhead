import { useState } from 'react'
import { Button } from '../../../design-system/components/Button'
import { Chip } from '../../../design-system/components/Chip'
import { RestrictedMarkdown } from '../../../design-system/components/RestrictedMarkdown'
import type { StudyItemDetailsDto } from '../api'
import type { BehavioralDetailsValue, LeetCodeDetailsValue, SystemDesignDetailsValue, TheoryDetailsValue } from './types'
import styles from './DetailsSummary.module.css'

// markdown fields (ApproachMarkdown, PromptMarkdown, ReferenceSolutionMarkdown, SummaryMarkdown)
// go through RestrictedMarkdown; everything else (Situation/Task/Action/Result, complexities,
// URLs) is a plain string per the domain model, not Markdown, and stays as-is.
function Row({ label, value, markdown }: { label: string; value: string; markdown?: boolean }) {
  return (
    <div className={styles.row}>
      <span className={styles.label}>{label}</span>
      {markdown ? <RestrictedMarkdown className={styles.value}>{value}</RestrictedMarkdown> : <p className={styles.value}>{value}</p>}
    </div>
  )
}

function TagsRow({ label, tags }: { label: string; tags: string[] }) {
  if (tags.length === 0) {
    return null
  }

  return (
    <div className={styles.row}>
      <span className={styles.label}>{label}</span>
      <div className={styles.tags}>
        {tags.map((tag) => (
          <Chip key={tag}>{tag}</Chip>
        ))}
      </div>
    </div>
  )
}

// Read-only rendering of the typed category variant (page-patterns.md "StudyItem detail and
// review"). SystemDesign's reference solution stays hidden behind transient component state
// until revealed, per the same section.
export function DetailsSummary({ details }: { details: StudyItemDetailsDto }) {
  switch (details.kind) {
    case 'LeetCode':
      return <LeetCodeSummary details={details} />
    case 'SystemDesign':
      return <SystemDesignSummary details={details} />
    case 'Behavioral':
      return <BehavioralSummary details={details} />
    case 'Theory':
      return <TheorySummary details={details} />
    default:
      return null
  }
}

function LeetCodeSummary({ details }: { details: LeetCodeDetailsValue }) {
  return (
    <div className={styles.summary}>
      <Row label="Difficulty" value={details.difficulty} />
      {details.problemNumber != null && <Row label="Problem number" value={String(details.problemNumber)} />}
      {details.url && <Row label="URL" value={details.url} />}
      <TagsRow label="Patterns" tags={details.patterns} />
      <Row label="Expected time complexity" value={details.expectedTimeComplexity} />
      <Row label="Expected space complexity" value={details.expectedSpaceComplexity} />
      <Row label="Approach" value={details.approachMarkdown} markdown />
      {details.cSharpSolution && <Row label="C# solution" value={details.cSharpSolution} />}
    </div>
  )
}

function SystemDesignSummary({ details }: { details: SystemDesignDetailsValue }) {
  const [revealed, setRevealed] = useState(false)

  return (
    <div className={styles.summary}>
      <Row label="Prompt" value={details.promptMarkdown} markdown />
      <TagsRow label="Clarifying questions" tags={details.clarifyingQuestions} />
      <TagsRow label="Functional requirements" tags={details.functionalRequirements} />
      <TagsRow label="Non-functional requirements" tags={details.nonFunctionalRequirements} />
      <TagsRow label="Evaluation checklist" tags={details.evaluationChecklist} />
      <div className={styles.row}>
        <span className={styles.label}>Reference solution</span>
        {revealed ? (
          <RestrictedMarkdown className={styles.value}>{details.referenceSolutionMarkdown}</RestrictedMarkdown>
        ) : (
          <Button type="button" variant="secondary" onClick={() => setRevealed(true)}>
            Reveal reference solution
          </Button>
        )}
      </div>
    </div>
  )
}

function BehavioralSummary({ details }: { details: BehavioralDetailsValue }) {
  return (
    <div className={styles.summary}>
      <TagsRow label="Competencies" tags={details.competencies} />
      <TagsRow label="Question variants" tags={details.questionVariants} />
      <Row label="Situation" value={details.situation} />
      <Row label="Task" value={details.task} />
      <Row label="Action" value={details.action} />
      <Row label="Result" value={details.result} />
      {details.reflection && <Row label="Reflection" value={details.reflection} />}
    </div>
  )
}

function TheorySummary({ details }: { details: TheoryDetailsValue }) {
  return (
    <div className={styles.summary}>
      <Row label="Summary" value={details.summaryMarkdown} markdown />
      <TagsRow label="Key points" tags={details.keyPoints} />
      <TagsRow label="Interview questions" tags={details.interviewQuestions} />
      <TagsRow label="References" tags={details.references} />
    </div>
  )
}
