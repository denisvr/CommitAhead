import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import { TagInput } from '../../../design-system/components/TagInput'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { EmploymentType, ExperienceEntryDto, SkillDto, WorkMode } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'
import { SkillPicker } from './SkillPicker'

type ExperienceEntryFieldsProps = {
  value: ExperienceEntryDto
  onChange: (value: ExperienceEntryDto) => void
  onRemove: () => void
  skills: SkillDto[]
}

const EMPLOYMENT_TYPES: EmploymentType[] = ['Permanent', 'Contract', 'Freelance', 'Internship', 'Other']
const WORK_MODES: WorkMode[] = ['OnSite', 'Hybrid', 'Remote', 'Other']

export function ExperienceEntryFields({ value, onChange, onRemove, skills }: ExperienceEntryFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Company">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.company} onChange={(event) => onChange({ ...value, company: event.target.value })} />
          )}
        </Field>
        <Field label="Client" hint="Optional — for contract/consulting work.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.client ?? ''}
              onChange={(event) => onChange({ ...value, client: event.target.value || null })}
            />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Role">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.role} onChange={(event) => onChange({ ...value, role: event.target.value })} />
          )}
        </Field>
        <Field label="Employment type">
          {(fieldProps) => (
            <select
              {...fieldProps}
              className={inputStyles.input}
              value={value.employmentType}
              onChange={(event) => onChange({ ...value, employmentType: event.target.value as EmploymentType })}
            >
              {EMPLOYMENT_TYPES.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Start date">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="month"
              required
              className={inputStyles.input}
              value={toMonthInputValue(value.startDate)}
              onChange={(event) => {
                const startDate = fromMonthInputValue(event.target.value)
                if (startDate) onChange({ ...value, startDate })
              }}
            />
          )}
        </Field>
        <Field label="End date" hint="Leave blank if this is your current role.">
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

      <div className={layout.row}>
        <Field label="Location" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.location ?? ''}
              onChange={(event) => onChange({ ...value, location: event.target.value || null })}
            />
          )}
        </Field>
        <Field label="Work mode">
          {(fieldProps) => (
            <select {...fieldProps} className={inputStyles.input} value={value.workMode} onChange={(event) => onChange({ ...value, workMode: event.target.value as WorkMode })}>
              {WORK_MODES.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          )}
        </Field>
      </div>

      <Field label="Summary">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.summaryMarkdown}
            onChange={(event) => onChange({ ...value, summaryMarkdown: event.target.value })}
          />
        )}
      </Field>

      <TagInput label="Achievements" value={value.achievements} onChange={(achievements) => onChange({ ...value, achievements })} />

      <SkillPicker label="Skills used" skills={skills} value={value.skillIds} onChange={(skillIds) => onChange({ ...value, skillIds })} />

      <Button type="button" variant="ghost" onClick={onRemove}>
        <Icon name="trash-2" /> Remove this role
      </Button>
    </div>
  )
}
