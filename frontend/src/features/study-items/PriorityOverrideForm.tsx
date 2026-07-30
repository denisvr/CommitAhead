import { useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import inputStyles from '../../design-system/components/Input.module.css'
import layout from './FormLayout.module.css'

type PriorityOverrideFormProps = {
  onSubmit: (score: number, reason: string) => Promise<void>
}

export function PriorityOverrideForm({ onSubmit }: PriorityOverrideFormProps) {
  const [score, setScore] = useState(50)
  const [reason, setReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await onSubmit(score, reason)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong setting the priority override.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={layout.stack} onSubmit={handleSubmit}>
      <div className={layout.row}>
        <Field label="Score" hint="0-100. Replaces the computed EffectiveScore.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={0}
              max={100}
              required
              className={inputStyles.input}
              value={score}
              onChange={(event) => setScore(Number(event.target.value))}
            />
          )}
        </Field>
        <Field label="Reason">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={reason} onChange={(event) => setReason(event.target.value)} />
          )}
        </Field>
      </div>
      {error && <p role="alert">{error}</p>}
      <Button type="submit" variant="secondary" isLoading={isSubmitting}>
        Set priority override
      </Button>
    </form>
  )
}
