import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { ContactInfoDto } from '../api'
import layout from '../FormLayout.module.css'

type ContactInfoFieldsProps = {
  value: ContactInfoDto
  onChange: (value: ContactInfoDto) => void
}

export function ContactInfoFields({ value, onChange }: ContactInfoFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Name">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.name} onChange={(event) => onChange({ ...value, name: event.target.value })} />
          )}
        </Field>
        <Field label="Email">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="email"
              required
              className={inputStyles.input}
              value={value.email}
              onChange={(event) => onChange({ ...value, email: event.target.value })}
            />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Phone" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="tel"
              className={inputStyles.input}
              value={value.phone ?? ''}
              onChange={(event) => onChange({ ...value, phone: event.target.value || null })}
            />
          )}
        </Field>
        <Field label="Address" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.address ?? ''}
              onChange={(event) => onChange({ ...value, address: event.target.value || null })}
            />
          )}
        </Field>
      </div>

      <Field label="Photo storage key" hint="Optional — a raw storage reference, not an upload widget yet.">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            className={inputStyles.input}
            value={value.photoStorageKey ?? ''}
            onChange={(event) => onChange({ ...value, photoStorageKey: event.target.value || null })}
          />
        )}
      </Field>
    </div>
  )
}
