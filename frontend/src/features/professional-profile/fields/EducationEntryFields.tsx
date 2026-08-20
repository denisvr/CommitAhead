import { Field } from '../../../design-system/components/Field'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { EducationEntryDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'

type EducationEntryFieldsProps = {
  value: EducationEntryDto
  onChange: (value: EducationEntryDto) => void
}

export function EducationEntryFields({ value, onChange }: EducationEntryFieldsProps) {
  return (
    <div className={layout.grid}>
      <Field label="Qualification" required>
        {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={value.degree} onChange={(event) => onChange({ ...value, degree: event.target.value })} />}
      </Field>
      <Field label="Institution" required>
        {(fieldProps) => (
          <input {...fieldProps} type="text" className={inputStyles.input} value={value.institution} onChange={(event) => onChange({ ...value, institution: event.target.value })} />
        )}
      </Field>

      <Field label="Field of study">
        {(fieldProps) => (
          <input {...fieldProps} type="text" className={inputStyles.input} value={value.field ?? ''} onChange={(event) => onChange({ ...value, field: event.target.value || null })} />
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

      <Field label="Subjects covered" inlineHint="optional" className={layout.wide}>
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.detailsMarkdown ?? ''}
            onChange={(event) => onChange({ ...value, detailsMarkdown: event.target.value || null })}
          />
        )}
      </Field>
    </div>
  )
}
