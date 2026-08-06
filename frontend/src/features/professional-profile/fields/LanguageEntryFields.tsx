import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { LanguageEntryDto, LanguageProficiency } from '../api'
import layout from '../FormLayout.module.css'

type LanguageEntryFieldsProps = {
  value: LanguageEntryDto
  onChange: (value: LanguageEntryDto) => void
  onRemove: () => void
}

const PROFICIENCIES: LanguageProficiency[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2', 'Native']

export function LanguageEntryFields({ value, onChange, onRemove }: LanguageEntryFieldsProps) {
  return (
    <div className={layout.row}>
      <Field label="Language">
        {(fieldProps) => (
          <input {...fieldProps} type="text" required className={inputStyles.input} value={value.language} onChange={(event) => onChange({ ...value, language: event.target.value })} />
        )}
      </Field>
      <Field label="Proficiency">
        {(fieldProps) => (
          <select
            {...fieldProps}
            className={inputStyles.input}
            value={value.proficiency}
            onChange={(event) => onChange({ ...value, proficiency: event.target.value as LanguageProficiency })}
          >
            {PROFICIENCIES.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label="Certification" hint="Optional.">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            className={inputStyles.input}
            value={value.certification ?? ''}
            onChange={(event) => onChange({ ...value, certification: event.target.value || null })}
          />
        )}
      </Field>
      <Button type="button" variant="ghost" onClick={onRemove} aria-label={`Remove ${value.language || 'language'}`}>
        <Icon name="trash-2" />
      </Button>
    </div>
  )
}
