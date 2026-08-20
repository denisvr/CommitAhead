import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { CertificationEntryDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'

type CertificationEntryFieldsProps = {
  value: CertificationEntryDto
  onChange: (value: CertificationEntryDto) => void
}

export function CertificationEntryFields({ value, onChange }: CertificationEntryFieldsProps) {
  return (
    <div className={layout.grid}>
      <Field label="Name" required>
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.name} onChange={(event) => onChange({ ...value, name: event.target.value })} />}
      </Field>
      <Field label="Issuing organisation" required>
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            className={inputStyles.input}
            value={value.issuingOrganisation}
            onChange={(event) => onChange({ ...value, issuingOrganisation: event.target.value })}
          />
        )}
      </Field>

      <Field label="Issued">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="month"
            className={inputStyles.input}
            value={toMonthInputValue(value.issuedAt)}
            onChange={(event) => onChange({ ...value, issuedAt: fromMonthInputValue(event.target.value) })}
          />
        )}
      </Field>
      <Field label="Expires">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="month"
            className={inputStyles.input}
            value={toMonthInputValue(value.expiresAt)}
            onChange={(event) => onChange({ ...value, expiresAt: fromMonthInputValue(event.target.value) })}
          />
        )}
      </Field>

      <Field label="Credential ID">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            className={inputStyles.input}
            value={value.credentialId ?? ''}
            onChange={(event) => onChange({ ...value, credentialId: event.target.value || null })}
          />
        )}
      </Field>
      <Field label="Verification URL" hint="A verifiable link makes this stronger on every CV that includes it.">
        {(fieldProps) => (
          <input {...fieldProps} type="url" className={inputStyles.input} value={value.url ?? ''} onChange={(event) => onChange({ ...value, url: event.target.value || null })} />
        )}
      </Field>
    </div>
  )
}
