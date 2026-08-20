import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { ProjectEntryDto, SkillDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'
import { SkillPicker } from './SkillPicker'

type ProjectEntryFieldsProps = {
  value: ProjectEntryDto
  onChange: (value: ProjectEntryDto) => void
  skills: SkillDto[]
}

export function ProjectEntryFields({ value, onChange, skills }: ProjectEntryFieldsProps) {
  return (
    <div className={layout.grid}>
      <Field label="Name" required className={layout.wide}>
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.name} onChange={(event) => onChange({ ...value, name: event.target.value })} />}
      </Field>

      <Field label="Role">
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.role ?? ''} onChange={(event) => onChange({ ...value, role: event.target.value || null })} />}
      </Field>
      <Field label="URL">
        {(fieldProps) => <input {...fieldProps} type="url" className={inputStyles.input} value={value.url ?? ''} onChange={(event) => onChange({ ...value, url: event.target.value || null })} />}
      </Field>

      <Field label="Start date">
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
      <Field label="End date">
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

      <Field label="Description" required className={layout.wide}>
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.descriptionMarkdown} onChange={(event) => onChange({ ...value, descriptionMarkdown: event.target.value })} />
        )}
      </Field>

      <div className={layout.wide}>
        <SkillPicker label="Technologies" skills={skills} value={value.skillIds} onChange={(skillIds) => onChange({ ...value, skillIds })} />
      </div>
    </div>
  )
}
