import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { RestrictedMarkdown } from '../../design-system/components/RestrictedMarkdown'
import inputStyles from '../../design-system/components/Input.module.css'
import type { SuggestionProposalResponse } from './api'
import {
  JOB_GAP_MATCH_LEVELS,
  JOB_GAP_SEVERITIES,
  JOB_REQUIREMENT_KINDS,
  JOB_REQUIREMENT_PRIORITIES,
  parseSuggestionFields,
  SUGGESTION_FIELD_SPECS,
  type SuggestionFields,
} from './payloadFields'
import { ProposedFieldsList } from './ProposedFieldsList'
import styles from './ProposalCard.module.css'

export type SuggestionDecisionState = { decided: boolean; accepted: boolean; fields: SuggestionFields }

type SuggestionProposalCardProps = {
  proposal: SuggestionProposalResponse
  decision: SuggestionDecisionState
  onChange: (decision: SuggestionDecisionState) => void
}

// components.md "ProposalDecision": shows the immutable proposal, a transient Accepted/Rejected
// choice, and — only when Accepted — the complete editable final payload.
export function SuggestionProposalCard({ proposal, decision, onChange }: SuggestionProposalCardProps) {
  const isAdvisory = proposal.proposedCommandType == null
  // Recomputed from the immutable proposal on every render — never from `decision.fields`, which
  // is the mutable in-progress edit of the accepted payload and must not double as "what AI
  // actually proposed."
  const proposedFields = proposal.proposedCommandType && proposal.proposedPayloadJson ? parseSuggestionFields(proposal.proposedCommandType, proposal.proposedPayloadJson) : {}

  return (
    <li className={styles.card}>
      <div className={styles.proposed}>
        {isAdvisory ? (
          <RestrictedMarkdown>{proposal.proposedAdvisoryMarkdown ?? ''}</RestrictedMarkdown>
        ) : (
          <>
            <p className={styles.commandLabel}>{proposal.proposedCommandType}</p>
            {proposal.proposedCommandType === 'AddJobGap' && (
              <p>Targets requirement: {proposal.targetRequirementText ?? '(no longer exists)'}</p>
            )}
            <ProposedFieldsList fields={proposedFields} specs={SUGGESTION_FIELD_SPECS[proposal.proposedCommandType!]} />
          </>
        )}
      </div>

      <div className={styles.decisionRow}>
        <Button variant={decision.decided && decision.accepted ? 'primary' : 'secondary'} onClick={() => onChange({ ...decision, decided: true, accepted: true })}>
          Accept
        </Button>
        <Button variant={decision.decided && !decision.accepted ? 'danger' : 'secondary'} onClick={() => onChange({ ...decision, decided: true, accepted: false })}>
          Reject
        </Button>
      </div>

      {decision.decided && decision.accepted && !isAdvisory && (
        <div className={styles.editableFields}>
          {proposal.proposedCommandType === 'AddJobRequirement' && (
            <>
              <Field label="Text">
                {(fieldProps) => (
                  <textarea {...fieldProps} rows={2} className={inputStyles.input} value={decision.fields.text ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, text: e.target.value } })} />
                )}
              </Field>
              <Field label="Kind">
                {(fieldProps) => (
                  <select {...fieldProps} className={inputStyles.input} value={decision.fields.kind ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, kind: e.target.value } })}>
                    {JOB_REQUIREMENT_KINDS.map((kind) => (
                      <option key={kind} value={kind}>
                        {kind}
                      </option>
                    ))}
                  </select>
                )}
              </Field>
              <Field label="Priority">
                {(fieldProps) => (
                  <select {...fieldProps} className={inputStyles.input} value={decision.fields.priority ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, priority: e.target.value } })}>
                    {JOB_REQUIREMENT_PRIORITIES.map((priority) => (
                      <option key={priority} value={priority}>
                        {priority}
                      </option>
                    ))}
                  </select>
                )}
              </Field>
              <Field label="Source excerpt">
                {(fieldProps) => (
                  <textarea {...fieldProps} rows={2} className={inputStyles.input} value={decision.fields.sourceExcerpt ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, sourceExcerpt: e.target.value } })} />
                )}
              </Field>
            </>
          )}

          {proposal.proposedCommandType === 'AddJobGap' && (
            <>
              <Field label="Match level">
                {(fieldProps) => (
                  <select {...fieldProps} className={inputStyles.input} value={decision.fields.matchLevel ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, matchLevel: e.target.value } })}>
                    {JOB_GAP_MATCH_LEVELS.map((level) => (
                      <option key={level} value={level}>
                        {level}
                      </option>
                    ))}
                  </select>
                )}
              </Field>
              <Field label="Severity">
                {(fieldProps) => (
                  <select {...fieldProps} className={inputStyles.input} value={decision.fields.severity ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, severity: e.target.value } })}>
                    {JOB_GAP_SEVERITIES.map((severity) => (
                      <option key={severity} value={severity}>
                        {severity}
                      </option>
                    ))}
                  </select>
                )}
              </Field>
              <Field label="Rationale">
                {(fieldProps) => (
                  <textarea {...fieldProps} rows={2} className={inputStyles.input} value={decision.fields.rationale ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, rationale: e.target.value } })} />
                )}
              </Field>
            </>
          )}

          {proposal.proposedCommandType === 'UpdateCVPresentationSummary' && (
            <Field label="Summary" hint="Leave blank to clear back to the profile's own summary.">
              {(fieldProps) => (
                <textarea {...fieldProps} rows={4} className={inputStyles.input} value={decision.fields.summaryMarkdown ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, summaryMarkdown: e.target.value } })} />
              )}
            </Field>
          )}

          {(proposal.proposedCommandType === 'AddInterviewGap' || proposal.proposedCommandType === 'AddInterviewLesson') && (
            <Field label="Text">
              {(fieldProps) => (
                <textarea {...fieldProps} rows={2} className={inputStyles.input} value={decision.fields.text ?? ''} onChange={(e) => onChange({ ...decision, fields: { ...decision.fields, text: e.target.value } })} />
              )}
            </Field>
          )}
        </div>
      )}
    </li>
  )
}
