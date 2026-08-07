import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import inputStyles from '../../design-system/components/Input.module.css'
import { fetchJobAnalyses, type JobAnalysisResponse } from '../job-analyses/api'
import layout from '../job-analyses/FormLayout.module.css'
import { createInterviewNote, toNumber, updateInterviewNote, type InterviewNoteResponse, type InterviewRound, type UpdateInterviewNoteRequest } from './api'
import styles from './InterviewNoteForm.module.css'
import { toLocalDateInputValue } from './localDate'
import { StringEntryListEditor } from './StringEntryListEditor'

type FormValues = UpdateInterviewNoteRequest & { jobAnalysisId: string | null }

const DEFAULT_VALUES: FormValues = {
  company: '',
  role: '',
  interviewRound: 'RecruiterScreening',
  sequenceNumber: 1,
  otherLabel: null,
  date: toLocalDateInputValue(new Date()),
  questions: [],
  gaps: [],
  lessons: [],
  jobAnalysisId: null,
}

const ROUNDS: InterviewRound[] = [
  'RecruiterScreening',
  'HiringManager',
  'Technical',
  'LiveCoding',
  'TakeHome',
  'SystemDesign',
  'Behavioral',
  'Panel',
  'Final',
  'Other',
]

function fromNote(note: InterviewNoteResponse): FormValues {
  return {
    company: note.company,
    role: note.role,
    interviewRound: note.interviewRound,
    sequenceNumber: toNumber(note.sequenceNumber),
    otherLabel: note.otherLabel,
    date: note.date,
    questions: note.questions,
    gaps: note.gaps,
    lessons: note.lessons,
    jobAnalysisId: note.jobAnalysisId,
  }
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

type CreateInterviewNoteFormProps = {
  mode: 'create'
  onCreated: (id: string) => void
  onCancel: () => void
}

type EditInterviewNoteFormProps = {
  mode: 'edit'
  note: InterviewNoteResponse
  onSaved: () => void
  onCancel: () => void
}

type InterviewNoteFormProps = CreateInterviewNoteFormProps | EditInterviewNoteFormProps

type JobAnalysesLoadState = 'loading' | 'ready' | 'error'

// components.md AppShell destination 5. Create and edit share one form (mirrors
// CVPresentationForm's own mode-prop convention) — CreateInterviewNoteRequest and
// UpdateInterviewNoteRequest have the identical shape on the backend.
export function InterviewNoteForm(props: InterviewNoteFormProps) {
  const [values, setValues] = useState<FormValues>(props.mode === 'edit' ? fromNote(props.note) : DEFAULT_VALUES)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [jobAnalysesLoadState, setJobAnalysesLoadState] = useState<JobAnalysesLoadState>('loading')
  const [jobAnalyses, setJobAnalyses] = useState<JobAnalysisResponse[]>([])

  useEffect(() => {
    fetchJobAnalyses()
      .then((data) => {
        setJobAnalyses(data)
        setJobAnalysesLoadState('ready')
      })
      .catch(() => {
        setJobAnalysesLoadState('error')
      })
  }, [])

  const handleSubmit = async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      if (props.mode === 'create') {
        const id = await createInterviewNote(values)
        props.onCreated(id)
      } else {
        await updateInterviewNote(props.note.id, values)
        props.onSaved()
      }
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong saving this interview note.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form
      className={layout.stack}
      onSubmit={(event) => {
        event.preventDefault()
        void handleSubmit()
      }}
    >
      {props.mode === 'create' && <h1 className={styles.title}>New interview note</h1>}

      <div className={layout.row}>
        <Field label="Company">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={values.company} onChange={(event) => setValues({ ...values, company: event.target.value })} />
          )}
        </Field>
        <Field label="Role">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={values.role} onChange={(event) => setValues({ ...values, role: event.target.value })} />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Round">
          {(fieldProps) => (
            <select
              {...fieldProps}
              className={inputStyles.input}
              value={values.interviewRound}
              onChange={(event) => {
                const interviewRound = event.target.value as InterviewRound
                setValues({ ...values, interviewRound, otherLabel: interviewRound === 'Other' ? values.otherLabel : null })
              }}
            >
              {ROUNDS.map((round) => (
                <option key={round} value={round}>
                  {round}
                </option>
              ))}
            </select>
          )}
        </Field>
        <Field label="Sequence number" hint="1 for the first round of this kind, 2 for a second, and so on.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={1}
              required
              className={inputStyles.input}
              value={values.sequenceNumber}
              onChange={(event) => setValues({ ...values, sequenceNumber: Number(event.target.value) })}
            />
          )}
        </Field>
        <Field label="Date">
          {(fieldProps) => (
            <input {...fieldProps} type="date" required className={inputStyles.input} value={values.date} onChange={(event) => setValues({ ...values, date: event.target.value })} />
          )}
        </Field>
      </div>

      {values.interviewRound === 'Other' && (
        <Field label="Other label" hint="Required when round is Other.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              required
              className={inputStyles.input}
              value={values.otherLabel ?? ''}
              onChange={(event) => setValues({ ...values, otherLabel: event.target.value })}
            />
          )}
        </Field>
      )}

      <Field label="Linked job analysis" hint="Optional.">
        {(fieldProps) => (
          <select
            {...fieldProps}
            className={inputStyles.input}
            value={values.jobAnalysisId ?? ''}
            disabled={jobAnalysesLoadState !== 'ready'}
            onChange={(event) => setValues({ ...values, jobAnalysisId: event.target.value || null })}
          >
            <option value="">None</option>
            {jobAnalyses.map((analysis) => (
              <option key={analysis.id} value={analysis.id}>
                {analysis.title}
              </option>
            ))}
          </select>
        )}
      </Field>
      {jobAnalysesLoadState === 'error' && <p role="alert">Could not load your job analyses — you can still save without linking one.</p>}

      <StringEntryListEditor
        label="Questions asked"
        values={values.questions}
        onChange={(questions) => setValues({ ...values, questions })}
        addLabel="Add question"
        emptyLabel="No questions recorded yet."
      />

      <StringEntryListEditor
        label="Gaps observed"
        values={values.gaps}
        onChange={(gaps) => setValues({ ...values, gaps })}
        addLabel="Add gap"
        emptyLabel="No gaps recorded yet."
      />

      <StringEntryListEditor
        label="Lessons learned"
        values={values.lessons}
        onChange={(lessons) => setValues({ ...values, lessons })}
        addLabel="Add lesson"
        emptyLabel="No lessons recorded yet."
      />

      {error && <p role="alert">{error}</p>}

      <div className={layout.row}>
        <Button type="submit" variant="primary" isLoading={isSubmitting}>
          {props.mode === 'create' ? 'Create' : 'Save'}
        </Button>
        <Button type="button" variant="ghost" onClick={props.onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
