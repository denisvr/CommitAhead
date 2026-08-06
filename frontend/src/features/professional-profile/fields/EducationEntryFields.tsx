import { Button } from '../../../design-system/components/Button'
import { Field } from '../../../design-system/components/Field'
import { Icon } from '../../../design-system/Icon'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { EducationEntryDto } from '../api'
import { fromMonthInputValue, toMonthInputValue } from '../yearMonth'
import layout from '../FormLayout.module.css'

type EducationEntryFieldsProps = {
  value: EducationEntryDto
  onChange: (value: EducationEntryDto) => void
  onRemove: () => void
}

export function EducationEntryFields({ value, onChange, onRemove }: EducationEntryFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Institution">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              required
              className={inputStyles.input}
              value={value.institution}
              onChange={(event) => onChange({ ...value, institution: event.target.value })}
            />
          )}
        </Field>
        <Field label="Degree">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={value.degree} onChange={(event) => onChange({ ...value, degree: event.target.value })} />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Field of study" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.field ?? ''}
              onChange={(event) => onChange({ ...value, field: event.target.value || null })}
            />
          )}
        </Field>
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

      <Field label="Details" hint="Optional.">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.detailsMarkdown ?? ''}
            onChange={(event) => onChange({ ...value, detailsMarkdown: event.target.value || null })}
          />
        )}
      </Field>

      <Button type="button" variant="ghost" onClick={onRemove}>
        <Icon name="trash-2" /> Remove this entry
      </Button>
    </div>
  )
}
