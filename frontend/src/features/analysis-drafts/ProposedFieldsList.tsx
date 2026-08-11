import { RestrictedMarkdown } from '../../design-system/components/RestrictedMarkdown'
import { splitLines, type FieldSpec } from './payloadFields'
import styles from './ProposalCard.module.css'

type ProposedFieldsListProps = {
  fields: Record<string, string>
  specs: FieldSpec[]
}

// Renders the immutable AI-proposed field values (or a finalised Accepted payload) read-only.
// Markdown fields keep going through RestrictedMarkdown even here — threat-model.md's "same
// pipeline, no exceptions" rule for AI-authored content applies to every rendering path, not only
// the ones that happen to be editable.
export function ProposedFieldsList({ fields, specs }: ProposedFieldsListProps) {
  return (
    <dl className={styles.fieldList}>
      {specs.map((spec) => {
        const value = fields[spec.key] ?? ''
        return (
          <div key={spec.key} className={styles.fieldRow}>
            <dt className={styles.fieldLabel}>{spec.label}</dt>
            <dd className={styles.fieldValue}>
              {spec.key.endsWith('Markdown') ? (
                <RestrictedMarkdown>{value}</RestrictedMarkdown>
              ) : spec.multiline ? (
                <ul>
                  {splitLines(value).map((line, index) => (
                    <li key={index}>{line}</li>
                  ))}
                </ul>
              ) : (
                <span>{value || '—'}</span>
              )}
            </dd>
          </div>
        )
      })}
    </dl>
  )
}
