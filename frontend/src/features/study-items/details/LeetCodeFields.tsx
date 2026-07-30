import { Field } from '../../../design-system/components/Field'
import { TagInput } from '../../../design-system/components/TagInput'
import inputStyles from '../../../design-system/components/Input.module.css'
import type { Difficulty } from '../api'
import type { LeetCodeDetailsValue } from './types'
import layout from '../FormLayout.module.css'

type LeetCodeFieldsProps = {
  value: LeetCodeDetailsValue
  onChange: (value: LeetCodeDetailsValue) => void
}

export function LeetCodeFields({ value, onChange }: LeetCodeFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Problem number">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={1}
              className={inputStyles.input}
              value={value.problemNumber ?? ''}
              onChange={(event) => onChange({ ...value, problemNumber: event.target.value ? Number(event.target.value) : null })}
            />
          )}
        </Field>
        <Field label="Difficulty">
          {(fieldProps) => (
            <select
              {...fieldProps}
              className={inputStyles.input}
              value={value.difficulty}
              onChange={(event) => onChange({ ...value, difficulty: event.target.value as Difficulty })}
            >
              <option value="Easy">Easy</option>
              <option value="Medium">Medium</option>
              <option value="Hard">Hard</option>
            </select>
          )}
        </Field>
      </div>

      <Field label="URL" hint="Optional link to the problem.">
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="url"
            className={inputStyles.input}
            value={value.url ?? ''}
            onChange={(event) => onChange({ ...value, url: event.target.value || null })}
          />
        )}
      </Field>

      <TagInput label="Patterns" value={value.patterns} onChange={(patterns) => onChange({ ...value, patterns })} />

      <div className={layout.row}>
        <Field label="Expected time complexity">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.expectedTimeComplexity}
              onChange={(event) => onChange({ ...value, expectedTimeComplexity: event.target.value })}
            />
          )}
        </Field>
        <Field label="Expected space complexity">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={value.expectedSpaceComplexity}
              onChange={(event) => onChange({ ...value, expectedSpaceComplexity: event.target.value })}
            />
          )}
        </Field>
      </div>

      <Field label="Approach">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.approachMarkdown}
            onChange={(event) => onChange({ ...value, approachMarkdown: event.target.value })}
          />
        )}
      </Field>

      <Field label="C# solution" hint="Optional.">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={value.cSharpSolution ?? ''}
            onChange={(event) => onChange({ ...value, cSharpSolution: event.target.value || null })}
          />
        )}
      </Field>
    </div>
  )
}
