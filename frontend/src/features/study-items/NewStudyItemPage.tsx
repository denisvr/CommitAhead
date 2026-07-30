import { useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { RatingScale } from '../../design-system/components/RatingScale'
import { TagInput } from '../../design-system/components/TagInput'
import inputStyles from '../../design-system/components/Input.module.css'
import { createStudyItem, type StudyItemCategory, type StudyItemDetailsDto } from './api'
import { DetailsFields } from './details/DetailsFields'
import { defaultDetailsFor } from './details/defaultDetails'
import layout from './FormLayout.module.css'
import styles from './NewStudyItemPage.module.css'

type NewStudyItemPageProps = {
  onCreated: (id: string) => void
  onCancel: () => void
}

const CATEGORIES: { value: StudyItemCategory; label: string }[] = [
  { value: 'Theory', label: 'Theory' },
  { value: 'LeetCode', label: 'LeetCode' },
  { value: 'SystemDesign', label: 'System design' },
  { value: 'Behavioral', label: 'Behavioral' },
]

export function NewStudyItemPage({ onCreated, onCancel }: NewStudyItemPageProps) {
  const [category, setCategory] = useState<StudyItemCategory>('Theory')
  const [title, setTitle] = useState('')
  const [importance, setImportance] = useState(3)
  const [initialMastery, setInitialMastery] = useState(1)
  const [tags, setTags] = useState<string[]>([])
  const [details, setDetails] = useState<StudyItemDetailsDto>(defaultDetailsFor('Theory'))
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleCategoryChange = (next: StudyItemCategory) => {
    setCategory(next)
    setDetails(defaultDetailsFor(next))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      const id = await createStudyItem({ title, category, importance, initialMastery, tags, details })
      onCreated(id)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong creating this study item.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={[styles.page, layout.stack].join(' ')} onSubmit={handleSubmit}>
      <h1 className={styles.title}>New study item</h1>

      <Field label="Category">
        {(fieldProps) => (
          <select {...fieldProps} className={inputStyles.input} value={category} onChange={(event) => handleCategoryChange(event.target.value as StudyItemCategory)}>
            {CATEGORIES.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        )}
      </Field>

      <Field label="Title">
        {(fieldProps) => <input {...fieldProps} type="text" required className={inputStyles.input} value={title} onChange={(event) => setTitle(event.target.value)} />}
      </Field>

      <div>
        <span className={styles.ratingLabel}>Importance</span>
        <RatingScale label="Importance" value={importance} onChange={setImportance} />
      </div>

      <div>
        <span className={styles.ratingLabel}>Initial mastery</span>
        <RatingScale label="Initial mastery" value={initialMastery} onChange={setInitialMastery} />
      </div>

      <TagInput label="Tags" value={tags} onChange={setTags} />

      <DetailsFields category={category} value={details} onChange={setDetails} />

      {error && <p role="alert">{error}</p>}

      <div className={styles.actions}>
        <Button type="submit" variant="primary" isLoading={isSubmitting}>
          Create
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
