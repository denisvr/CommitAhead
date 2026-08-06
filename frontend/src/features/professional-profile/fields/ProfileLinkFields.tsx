import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { ProfileLinkDto, ProfileLinkKind } from '../api'
import layout from '../FormLayout.module.css'

type ProfileLinkFieldsProps = {
  value: ProfileLinkDto
  onChange: (value: ProfileLinkDto) => void
  onRemove: () => void
}

const KINDS: ProfileLinkKind[] = ['GitHub', 'LinkedIn', 'Portfolio', 'Blog', 'Other']

export function ProfileLinkFields({ value, onChange, onRemove }: ProfileLinkFieldsProps) {
  return (
    <div className={layout.row}>
      <Field label="Kind">
        {(fieldProps) => (
          <select {...fieldProps} className={inputStyles.input} value={value.kind} onChange={(event) => onChange({ ...value, kind: event.target.value as ProfileLinkKind })}>
            {KINDS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label="Label" hint="Optional.">
        {(fieldProps) => (
          <input {...fieldProps} type="text" className={inputStyles.input} value={value.label ?? ''} onChange={(event) => onChange({ ...value, label: event.target.value || null })} />
        )}
      </Field>
      <Field label="URL">
        {(fieldProps) => (
          <input {...fieldProps} type="url" required className={inputStyles.input} value={value.url} onChange={(event) => onChange({ ...value, url: event.target.value })} />
        )}
      </Field>
      <Button type="button" variant="ghost" onClick={onRemove} aria-label={`Remove ${value.label || value.kind}`}>
        <Icon name="trash-2" />
      </Button>
    </div>
  )
}
