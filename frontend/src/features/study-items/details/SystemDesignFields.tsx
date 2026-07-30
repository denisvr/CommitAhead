import { Field } from '../../../design-system/components/Field'
import { TagInput } from '../../../design-system/components/TagInput'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { SystemDesignDetailsValue } from './types'
import layout from '../FormLayout.module.css'

type SystemDesignFieldsProps = {
  value: SystemDesignDetailsValue
  onChange: (value: SystemDesignDetailsValue) => void
}

export function SystemDesignFields({ value, onChange }: SystemDesignFieldsProps) {
  return (
    <div className={layout.stack}>
      <Field label="Prompt">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.promptMarkdown}
            onChange={(event) => onChange({ ...value, promptMarkdown: event.target.value })}
          />
        )}
      </Field>

      <TagInput label="Clarifying questions" value={value.clarifyingQuestions} onChange={(clarifyingQuestions) => onChange({ ...value, clarifyingQuestions })} />
      <TagInput
        label="Functional requirements"
        value={value.functionalRequirements}
        onChange={(functionalRequirements) => onChange({ ...value, functionalRequirements })}
      />
      <TagInput
        label="Non-functional requirements"
        value={value.nonFunctionalRequirements}
        onChange={(nonFunctionalRequirements) => onChange({ ...value, nonFunctionalRequirements })}
      />
      <TagInput label="Evaluation checklist" value={value.evaluationChecklist} onChange={(evaluationChecklist) => onChange({ ...value, evaluationChecklist })} />

      <Field label="Reference solution">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.referenceSolutionMarkdown}
            onChange={(event) => onChange({ ...value, referenceSolutionMarkdown: event.target.value })}
          />
        )}
      </Field>
    </div>
  )
}
