import { Field } from '../../../design-system/components/Field'
import { TagInput } from '../../../design-system/components/TagInput'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { TheoryDetailsValue } from './types'
import layout from '../FormLayout.module.css'

type TheoryFieldsProps = {
  value: TheoryDetailsValue
  onChange: (value: TheoryDetailsValue) => void
}

export function TheoryFields({ value, onChange }: TheoryFieldsProps) {
  return (
    <div className={layout.stack}>
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

      <TagInput label="Key points" value={value.keyPoints} onChange={(keyPoints) => onChange({ ...value, keyPoints })} />
      <TagInput label="Interview questions" value={value.interviewQuestions} onChange={(interviewQuestions) => onChange({ ...value, interviewQuestions })} />
      <TagInput label="References" value={value.references} onChange={(references) => onChange({ ...value, references })} />
    </div>
  )
}
