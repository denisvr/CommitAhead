import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import inputStyles from '../../design-system/components/Input.module.css'
import type { LinkProposalResponse } from './api'
import styles from './ProposalCard.module.css'

export type LinkDecisionState = { decided: boolean; accepted: boolean; weight: string; rationale: string }

type LinkProposalCardProps = {
  proposal: LinkProposalResponse
  decision: LinkDecisionState
  onChange: (decision: LinkDecisionState) => void
}

export function LinkProposalCard({ proposal, decision, onChange }: LinkProposalCardProps) {
  return (
    <li className={styles.card}>
      <div className={styles.proposed}>
        <p className={styles.commandLabel}>Link to {proposal.targetStudyItemTitle ?? `StudyItem ${proposal.targetStudyItemId}`}</p>
        <p>Proposed weight: {proposal.proposedWeight} — {proposal.proposedRationale}</p>
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
          <Field label="Weight (0-5)">
            {(fieldProps) => (
              <input {...fieldProps} type="number" min={0} max={5} step={0.1} className={inputStyles.input} value={decision.weight} onChange={(e) => onChange({ ...decision, weight: e.target.value })} />
            )}
          </Field>
          <Field label="Rationale">
            {(fieldProps) => (
              <textarea {...fieldProps} rows={2} className={inputStyles.input} value={decision.rationale} onChange={(e) => onChange({ ...decision, rationale: e.target.value })} />
            )}
          </Field>
        </div>
      )}
    </li>
  )
}
