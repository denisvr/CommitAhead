import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { ProfileLinkDto, ProfileLinkKind } from '../api'
import layout from '../FormLayout.module.css'

type ProfileLinkFieldsProps = {
  value: ProfileLinkDto
  onChange: (value: ProfileLinkDto) => void
}

const KINDS: ProfileLinkKind[] = ['GitHub', 'LinkedIn', 'Portfolio', 'Blog', 'Other']

// Rendered inside LinksSection's chip editor — deletion lives on the chip itself (its own ✕ and
// the editor's Delete button), so this only ever edits, never removes.
export function ProfileLinkFields({ value, onChange }: ProfileLinkFieldsProps) {
  return (
    <div className={layout.grid}>
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
      <Field label="URL" className={layout.wide}>
        {(fieldProps) => (
          <input {...fieldProps} type="url" required className={inputStyles.input} value={value.url} onChange={(event) => onChange({ ...value, url: event.target.value })} />
        )}
      </Field>
    </div>
  )
}
