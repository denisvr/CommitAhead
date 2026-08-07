import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { Tabs } from '../../design-system/components/Tabs'
import inputStyles from '../../design-system/components/Input.module.css'
import { createJobAnalysis, createJobAnalysisFromUpload } from './api'
import layout from './FormLayout.module.css'
import styles from './NewJobAnalysisPage.module.css'

type NewJobAnalysisPageProps = {
  onCreated: (id: string) => void
  onCancel: () => void
}

type SourceMode = 'paste' | 'upload'

const SOURCE_TABS = [
  { key: 'paste', label: 'Paste text' },
  { key: 'upload', label: 'Upload PDF' },
]

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

// components.md AppShell destination 4 — the two JobSource provenance paths (ADR-0002/ADR-0010)
// share one create page. Only pasted text goes through the plain JSON endpoint; a PDF upload goes
// through the separate multipart endpoint, whose own use case is the only thing trusted to build
// an UploadedFile — the client never constructs one itself.
export function NewJobAnalysisPage({ onCreated, onCancel }: NewJobAnalysisPageProps) {
  const [sourceMode, setSourceMode] = useState<SourceMode>('paste')
  const [title, setTitle] = useState('')
  const [notesMarkdown, setNotesMarkdown] = useState('')
  const [jobPostingText, setJobPostingText] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    setFile(event.target.files?.[0] ?? null)
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (sourceMode === 'upload' && !file) {
      setError('Choose a PDF file to upload.')
      return
    }

    setIsSubmitting(true)

    try {
      const id =
        sourceMode === 'paste'
          ? await createJobAnalysis({ title, jobPostingText, notesMarkdown: notesMarkdown || null })
          : await createJobAnalysisFromUpload(title, file!, notesMarkdown || null)
      onCreated(id)
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong creating this job analysis.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={[styles.page, layout.stack].join(' ')} onSubmit={handleSubmit}>
      <h1 className={styles.title}>New job analysis</h1>

      <Field label="Title" hint="A name to tell this analysis apart from others, e.g. &quot;Acme — Senior Backend Engineer&quot;.">
        {(fieldProps) => <input {...fieldProps} type="text" required className={inputStyles.input} value={title} onChange={(event) => setTitle(event.target.value)} />}
      </Field>

      <Tabs tabs={SOURCE_TABS} activeTab={sourceMode} onChange={(key) => setSourceMode(key as SourceMode)} aria-label="Job posting source" />

      <div id={`tabpanel-${sourceMode}`} role="tabpanel" aria-labelledby={`tab-${sourceMode}`}>
        {sourceMode === 'paste' ? (
          <Field label="Job posting text">
            {(fieldProps) => (
              <textarea
                {...fieldProps}
                required
                rows={10}
                className={inputStyles.input}
                value={jobPostingText}
                onChange={(event) => setJobPostingText(event.target.value)}
              />
            )}
          </Field>
        ) : (
          <Field label="PDF file" hint="Up to 5 MB, up to 20 pages. Text-only extraction — no images, scripts, or embedded links are processed.">
            {(fieldProps) => <input {...fieldProps} type="file" accept="application/pdf,.pdf" className={inputStyles.input} onChange={handleFileChange} />}
          </Field>
        )}
      </div>

      <Field label="Notes" hint="Optional.">
        {(fieldProps) => (
          <textarea {...fieldProps} rows={4} className={inputStyles.input} value={notesMarkdown} onChange={(event) => setNotesMarkdown(event.target.value)} />
        )}
      </Field>

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
