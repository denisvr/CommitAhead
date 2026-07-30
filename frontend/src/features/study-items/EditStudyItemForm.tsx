import { useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { RatingScale } from '../../design-system/components/RatingScale'
import { TagInput } from '../../design-system/components/TagInput'
import inputStyles from '../../design-system/components/Input.module.css'
import { toNumber, updateStudyItem, type StudyItemDetailsDto, type StudyItemResponse } from './api'
import { DetailsFields } from './details/DetailsFields'
import layout from './FormLayout.module.css'
import styles from './NewStudyItemPage.module.css'

type EditStudyItemFormProps = {
  item: StudyItemResponse
  onSaved: () => void
  onCancel: () => void
}

// Category is fixed once a StudyItem is created (ADR-0001) — Update never changes it, so this
// form only edits Title/Importance/Tags/Details.
export function EditStudyItemForm({ item, onSaved, onCancel }: EditStudyItemFormProps) {
  const [title, setTitle] = useState(item.title)
  const [importance, setImportance] = useState(toNumber(item.importance))
  const [tags, setTags] = useState<string[]>(item.tags)
  const [details, setDetails] = useState<StudyItemDetailsDto>(item.details)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await updateStudyItem(item.id, { title, importance, tags, details })
      onSaved()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong saving this study item.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={[styles.page, layout.stack].join(' ')} onSubmit={handleSubmit}>
      <h1 className={styles.title}>Edit study item</h1>

      <Field label="Title">
        {(fieldProps) => <input {...fieldProps} type="text" required className={inputStyles.input} value={title} onChange={(event) => setTitle(event.target.value)} />}
      </Field>

      <div>
        <span className={styles.ratingLabel}>Importance</span>
        <RatingScale label="Importance" value={importance} onChange={setImportance} />
      </div>

      <TagInput label="Tags" value={tags} onChange={setTags} />

      <DetailsFields category={item.category} value={details} onChange={setDetails} />

      {error && <p role="alert">{error}</p>}

      <div className={styles.actions}>
        <Button type="submit" variant="primary" isLoading={isSubmitting}>
          Save
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
