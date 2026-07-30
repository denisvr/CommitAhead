import { Field } from '../../../design-system/components/Field'
import { TagInput } from '../../../design-system/components/TagInput'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { BehavioralDetailsValue } from './types'
import layout from '../FormLayout.module.css'

type BehavioralFieldsProps = {
  value: BehavioralDetailsValue
  onChange: (value: BehavioralDetailsValue) => void
}

export function BehavioralFields({ value, onChange }: BehavioralFieldsProps) {
  return (
    <div className={layout.stack}>
      <TagInput label="Competencies" value={value.competencies} onChange={(competencies) => onChange({ ...value, competencies })} />
      <TagInput label="Question variants" value={value.questionVariants} onChange={(questionVariants) => onChange({ ...value, questionVariants })} />

      <Field label="Situation">
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.situation} onChange={(event) => onChange({ ...value, situation: event.target.value })} />
        )}
      </Field>
      <Field label="Task">
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.task} onChange={(event) => onChange({ ...value, task: event.target.value })} />
        )}
      </Field>
      <Field label="Action">
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.action} onChange={(event) => onChange({ ...value, action: event.target.value })} />
        )}
      </Field>
      <Field label="Result">
        {(fieldProps) => (
          <textarea {...fieldProps} className={inputStyles.input} value={value.result} onChange={(event) => onChange({ ...value, result: event.target.value })} />
        )}
      </Field>
      <Field label="Reflection" hint="Optional.">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.reflection ?? ''}
            onChange={(event) => onChange({ ...value, reflection: event.target.value || null })}
          />
        )}
      </Field>
    </div>
  )
}
