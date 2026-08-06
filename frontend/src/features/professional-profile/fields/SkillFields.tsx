import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { SkillCategory, SkillDto, SkillProficiency } from '../api'
import layout from '../FormLayout.module.css'

type SkillFieldsProps = {
  value: SkillDto
  onChange: (value: SkillDto) => void
  onRemove: () => void
}

const CATEGORIES: SkillCategory[] = [
  'Language',
  'Framework',
  'Platform',
  'Cloud',
  'Database',
  'Messaging',
  'DevOps',
  'Testing',
  'Architecture',
  'Tool',
  'Methodology',
  'Domain',
  'Other',
]

const PROFICIENCIES: SkillProficiency[] = ['Beginner', 'Intermediate', 'Advanced', 'Expert']

export function SkillFields({ value, onChange, onRemove }: SkillFieldsProps) {
  return (
    <div className={layout.row}>
      <Field label="Skill">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            required
            className={inputStyles.input}
            value={value.displayName}
            onChange={(event) => onChange({ ...value, displayName: event.target.value })}
          />
        )}
      </Field>
      <Field label="Category">
        {(fieldProps) => (
          <select {...fieldProps} className={inputStyles.input} value={value.category} onChange={(event) => onChange({ ...value, category: event.target.value as SkillCategory })}>
            {CATEGORIES.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label="Proficiency" hint="Optional.">
        {(fieldProps) => (
          <select
            {...fieldProps}
            className={inputStyles.input}
            value={value.proficiency ?? ''}
            onChange={(event) => onChange({ ...value, proficiency: (event.target.value || null) as SkillProficiency })}
          >
            <option value="">Unspecified</option>
            {PROFICIENCIES.map((option) => (
              <option key={option} value={option ?? ''}>
                {option}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Button type="button" variant="ghost" onClick={onRemove} aria-label={`Remove ${value.displayName || 'skill'}`}>
        <Icon name="trash-2" />
      </Button>
    </div>
  )
}
