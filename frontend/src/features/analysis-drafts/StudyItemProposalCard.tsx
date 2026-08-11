import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { RatingScale } from '../../design-system/components/RatingScale'
import { TagInput } from '../../design-system/components/TagInput'
import inputStyles from '../../design-system/components/Input.module.css'
import type { StudyItemProposalResponse } from './api'
import { parseStudyItemDetailsFields, STUDY_ITEM_DETAIL_FIELD_SPECS, studyItemProposedFieldSpecs, type StudyItemDetailsFields } from './payloadFields'
import { ProposedFieldsList } from './ProposedFieldsList'
import styles from './ProposalCard.module.css'

export type StudyItemDecisionState = {
  decided: boolean
  accepted: boolean
  title: string
  detailsFields: StudyItemDetailsFields
  tags: string[]
  importance: number
  initialMastery: number
}

type StudyItemProposalCardProps = {
  proposal: StudyItemProposalResponse
  decision: StudyItemDecisionState
  onChange: (decision: StudyItemDecisionState) => void
}

// Category itself is not editable here — accepting with a different category would need different
// details fields entirely; this slice keeps the proposed category fixed and lets every other field
// (title, details, tags, importance) be finalised, plus InitialMastery, which AI can never propose.
export function StudyItemProposalCard({ proposal, decision, onChange }: StudyItemProposalCardProps) {
  const fieldSpecs = STUDY_ITEM_DETAIL_FIELD_SPECS[proposal.proposedCategory]
  // Recomputed from the immutable proposal, not `decision.detailsFields` (the mutable in-progress
  // edit of the accepted payload) — same reasoning as SuggestionProposalCard.
  const proposedDetailsFields = parseStudyItemDetailsFields(proposal.proposedCategory, proposal.proposedDetailsJson)

  return (
    <li className={styles.card}>
      <div className={styles.proposed}>
        <p className={styles.commandLabel}>
          {proposal.proposedCategory} — {proposal.proposedTitle}
        </p>
        <ProposedFieldsList fields={proposedDetailsFields} specs={studyItemProposedFieldSpecs(proposal.proposedCategory)} />
        <p>Tags: {proposal.proposedTags.length > 0 ? proposal.proposedTags.join(', ') : '—'}</p>
        <p>Importance: {proposal.proposedImportance}</p>
      </div>

      <div className={styles.decisionRow}>
        <Button variant={decision.decided && decision.accepted ? 'primary' : 'secondary'} onClick={() => onChange({ ...decision, decided: true, accepted: true })}>
          Accept
        </Button>
        <Button variant={decision.decided && !decision.accepted ? 'danger' : 'secondary'} onClick={() => onChange({ ...decision, decided: true, accepted: false })}>
          Reject
        </Button>
      </div>

      {decision.decided && decision.accepted && (
        <div className={styles.editableFields}>
          <Field label="Title">
            {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={decision.title} onChange={(e) => onChange({ ...decision, title: e.target.value })} />}
          </Field>

          {fieldSpecs.map((spec) => (
            <Field key={spec.key} label={spec.label}>
              {(fieldProps) => {
                const value = decision.detailsFields[spec.key] ?? ''
                const setValue = (next: string) => onChange({ ...decision, detailsFields: { ...decision.detailsFields, [spec.key]: next } })

                if (spec.input === 'select') {
                  return (
                    <select {...fieldProps} className={inputStyles.input} value={value} onChange={(e) => setValue(e.target.value)}>
                      {(spec.options ?? []).map((option) => (
                        <option key={option} value={option}>
                          {option}
                        </option>
                      ))}
                    </select>
                  )
                }

                if (spec.input === 'textarea' || spec.input === 'multiline') {
                  return <textarea {...fieldProps} rows={spec.input === 'multiline' ? 3 : 4} className={inputStyles.input} value={value} onChange={(e) => setValue(e.target.value)} />
                }

                return <input {...fieldProps} type="text" className={inputStyles.input} value={value} onChange={(e) => setValue(e.target.value)} />
              }}
            </Field>
          ))}

          <TagInput label="Tags" value={decision.tags} onChange={(tags) => onChange({ ...decision, tags })} />
          <RatingScale label="Importance" value={decision.importance} onChange={(importance) => onChange({ ...decision, importance })} />
          <RatingScale label="Initial mastery" value={decision.initialMastery} onChange={(initialMastery) => onChange({ ...decision, initialMastery })} />
        </div>
      )}
    </li>
  )
}
