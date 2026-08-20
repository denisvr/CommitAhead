import { AchievementRepository } from '../../../design-system/components/AchievementRepository'
import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { EmploymentType, ExperienceEntryDto, SkillDto, WorkMode } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'
import checkStyles from './fieldChecks.module.css'
import { SkillPicker } from './SkillPicker'

type ExperienceEntryFieldsProps = {
  value: ExperienceEntryDto
  onChange: (value: ExperienceEntryDto) => void
  skills: SkillDto[]
  onHighlightAchievement?: (index: number | null) => void
}

const EMPLOYMENT_TYPES: EmploymentType[] = ['Permanent', 'Contract', 'Freelance', 'Internship', 'Other']
const WORK_MODES: WorkMode[] = ['OnSite', 'Hybrid', 'Remote', 'Other']

export function ExperienceEntryFields({ value, onChange, skills, onHighlightAchievement }: ExperienceEntryFieldsProps) {
  return (
    <div className={layout.grid}>
      <Field label="Role" required>
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.role} onChange={(event) => onChange({ ...value, role: event.target.value })} />}
      </Field>
      <Field label="Company" required>
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.company} onChange={(event) => onChange({ ...value, company: event.target.value })} />}
      </Field>

      <Field label="Client" inlineHint="for contract/consulting work">
        {(fieldProps) => (
          <input {...fieldProps} type="text" className={inputStyles.input} value={value.client ?? ''} onChange={(event) => onChange({ ...value, client: event.target.value || null })} />
        )}
      </Field>
      <Field label="Location">
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
      <Field label="Work arrangement">
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

      <Field label="From" required>
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="month"
            className={inputStyles.input}
            value={toMonthInputValue(value.startDate)}
            onChange={(event) => {
              const startDate = fromMonthInputValue(event.target.value)
              if (startDate) onChange({ ...value, startDate })
            }}
          />
        )}
      </Field>
      <Field label="To">
        {(fieldProps) => (
          <>
            <input
              {...fieldProps}
              type="month"
              disabled={value.endDate === null}
              className={inputStyles.input}
              value={toMonthInputValue(value.endDate)}
              onChange={(event) => onChange({ ...value, endDate: fromMonthInputValue(event.target.value) })}
            />
            <label className={checkStyles.check}>
              <input
                type="checkbox"
                checked={value.endDate === null}
                onChange={(event) => onChange({ ...value, endDate: event.target.checked ? null : { year: new Date().getFullYear(), month: new Date().getMonth() + 1 } })}
              />
              I still work here
            </label>
          </>
        )}
      </Field>

      <Field label="Context" inlineHint="what the company does and what you were brought in to do" className={layout.wide}>
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.summaryMarkdown} onChange={(event) => onChange({ ...value, summaryMarkdown: event.target.value })} />
        )}
      </Field>

      <div className={layout.wide}>
        <AchievementRepository achievements={value.achievements} onChange={(achievements) => onChange({ ...value, achievements })} onHighlight={onHighlightAchievement} />
      </div>

      <div className={layout.wide}>
        <SkillPicker label="Technologies" skills={skills} value={value.skillIds} onChange={(skillIds) => onChange({ ...value, skillIds })} />
      </div>
    </div>
  )
}
