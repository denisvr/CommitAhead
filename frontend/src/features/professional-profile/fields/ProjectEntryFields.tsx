import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { ProjectEntryDto, SkillDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'
import { SkillPicker } from './SkillPicker'

type ProjectEntryFieldsProps = {
  value: ProjectEntryDto
  onChange: (value: ProjectEntryDto) => void
  onRemove: () => void
  skills: SkillDto[]
}

export function ProjectEntryFields({ value, onChange, onRemove, skills }: ProjectEntryFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Name">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.name} onChange={(event) => onChange({ ...value, name: event.target.value })} />
          )}
        </Field>
        <Field label="Role" hint="Optional.">
          {(fieldProps) => (
            <input {...fieldProps} type="text" className={inputStyles.input} value={value.role ?? ''} onChange={(event) => onChange({ ...value, role: event.target.value || null })} />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Start date" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="month"
              className={inputStyles.input}
              value={toMonthInputValue(value.startDate)}
              onChange={(event) => onChange({ ...value, startDate: fromMonthInputValue(event.target.value) })}
            />
          )}
        </Field>
        <Field label="End date" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="month"
              className={inputStyles.input}
              value={toMonthInputValue(value.endDate)}
              onChange={(event) => onChange({ ...value, endDate: fromMonthInputValue(event.target.value) })}
            />
          )}
        </Field>
      </div>

      <Field label="Description">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.descriptionMarkdown}
            onChange={(event) => onChange({ ...value, descriptionMarkdown: event.target.value })}
          />
        )}
      </Field>

      <Field label="URL" hint="Optional.">
        {(fieldProps) => (
          <input {...fieldProps} type="url" className={inputStyles.input} value={value.url ?? ''} onChange={(event) => onChange({ ...value, url: event.target.value || null })} />
        )}
      </Field>

      <SkillPicker label="Skills used" skills={skills} value={value.skillIds} onChange={(skillIds) => onChange({ ...value, skillIds })} />

      <Button type="button" variant="ghost" onClick={onRemove}>
        <Icon name="trash-2" /> Remove this project
      </Button>
    </div>
  )
}
