import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { CertificationEntryDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'

type CertificationEntryFieldsProps = {
  value: CertificationEntryDto
  onChange: (value: CertificationEntryDto) => void
  onRemove: () => void
}

export function CertificationEntryFields({ value, onChange, onRemove }: CertificationEntryFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Name">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.name} onChange={(event) => onChange({ ...value, name: event.target.value })} />
          )}
        </Field>
        <Field label="Issuing organisation">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              required
              className={inputStyles.input}
              value={value.issuingOrganisation}
              onChange={(event) => onChange({ ...value, issuingOrganisation: event.target.value })}
            />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Issued" hint="Optional.">
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
        <Field label="Expires" hint="Optional.">
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
      </div>

      <div className={layout.row}>
        <Field label="Credential ID" hint="Optional.">
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
        <Field label="URL" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="url"
              className={inputStyles.input}
              value={value.url ?? ''}
              onChange={(event) => onChange({ ...value, url: event.target.value || null })}
            />
          )}
        </Field>
      </div>

      <Button type="button" variant="ghost" onClick={onRemove}>
        <Icon name="trash-2" /> Remove this certification
      </Button>
    </div>
  )
}
