import { useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { RatingScale } from '../../design-system/components/RatingScale'
import inputStyles from '../../design-system/components/Input.module.css'
import layout from './FormLayout.module.css'
import styles from './NewStudyItemPage.module.css'

type ReviewFormProps = {
  onSubmit: (confidenceRating: number, notesMarkdown: string | null) => Promise<void>
}

// Captures confidence 1-5 and optional notes (page-patterns.md "StudyItem detail and review");
// the caller refreshes Mastery/EffectiveScore from the server response after a successful save.
export function ReviewForm({ onSubmit }: ReviewFormProps) {
  const [confidenceRating, setConfidenceRating] = useState(3)
  const [notes, setNotes] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await onSubmit(confidenceRating, notes.trim() || null)
      setNotes('')
      setConfidenceRating(3)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong saving this review.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={layout.stack} onSubmit={handleSubmit}>
      <div>
        <span className={styles.ratingLabel}>Confidence</span>
        <RatingScale label="Confidence" value={confidenceRating} onChange={setConfidenceRating} />
      </div>
      <Field label="Notes" hint="Optional.">
        {(fieldProps) => <textarea {...fieldProps} className={inputStyles.input} value={notes} onChange={(event) => setNotes(event.target.value)} />}
      </Field>
      {error && <p role="alert">{error}</p>}
      <Button type="submit" variant="primary" isLoading={isSubmitting}>
        Save review
      </Button>
    </form>
  )
}
