import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import fieldStyles from '../../design-system/components/Field.module.css'
import inputStyles from '../../design-system/components/Input.module.css'
import { fetchProfessionalProfile, toNumber as toProfileNumber } from '../professional-profile/api'
import { createCVPresentation, toNumber, updateCVPresentation, type CVPresentationResponse, type UpdateCVPresentationRequest } from './api'
import layout from './FormLayout.module.css'

type FormValues = UpdateCVPresentationRequest

// The only template the backend export renderer actually renders (ExportCVPresentationUseCase's
// own SupportedTemplateKey) — export rejects any other value explicitly rather than silently
// rendering this one template anyway, so the form must not offer a value export can't honour.
const SUPPORTED_TEMPLATE_KEY = 'modern-one-page'

const DEFAULT_VALUES: FormValues = {
  label: '',
  targetMarket: '',
  targetRole: null,
  locale: 'en-GB',
  templateKey: SUPPORTED_TEMPLATE_KEY,
  summaryOverrideMarkdown: null,
  includePhoto: false,
  includeEmail: true,
  includePhone: true,
  includeAddress: false,
  dateFormat: 'dd MMM yyyy',
  pageLimit: 2,
}

function fromPresentation(presentation: CVPresentationResponse): FormValues {
  return {
    label: presentation.label,
    targetMarket: presentation.targetMarket,
    targetRole: presentation.targetRole,
    locale: presentation.locale,
    templateKey: presentation.templateKey,
    summaryOverrideMarkdown: presentation.summaryOverrideMarkdown,
    includePhoto: presentation.includePhoto,
    includeEmail: presentation.includeEmail,
    includePhone: presentation.includePhone,
    includeAddress: presentation.includeAddress,
    dateFormat: presentation.dateFormat,
    pageLimit: toNumber(presentation.pageLimit),
  }
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

type CVPresentationFormFieldsProps = {
  values: FormValues
  onChange: (values: FormValues) => void
}

function CVPresentationFormFields({ values, onChange }: CVPresentationFormFieldsProps) {
  return (
    <div className={layout.stack}>
      <div className={layout.row}>
        <Field label="Label" hint="A name to tell this presentation apart from others, e.g. &quot;UK — Senior Backend Engineer&quot;.">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={values.label} onChange={(event) => onChange({ ...values, label: event.target.value })} />
          )}
        </Field>
        <Field label="Target market">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              required
              className={inputStyles.input}
              value={values.targetMarket}
              onChange={(event) => onChange({ ...values, targetMarket: event.target.value })}
            />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Target role" hint="Optional.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              className={inputStyles.input}
              value={values.targetRole ?? ''}
              onChange={(event) => onChange({ ...values, targetRole: event.target.value || null })}
            />
          )}
        </Field>
        <Field label="Locale" hint="BCP-47 tag, e.g. en-GB or de-DE.">
          {(fieldProps) => (
            <input {...fieldProps} type="text" required className={inputStyles.input} value={values.locale} onChange={(event) => onChange({ ...values, locale: event.target.value })} />
          )}
        </Field>
      </div>

      <div className={layout.row}>
        <Field label="Template" hint="Only one template is available for now.">
          {(fieldProps) => (
            <select {...fieldProps} className={inputStyles.input} value={values.templateKey} disabled>
              <option value={values.templateKey}>{values.templateKey === SUPPORTED_TEMPLATE_KEY ? 'Modern — One Page' : values.templateKey}</option>
            </select>
          )}
        </Field>
        <Field label="Date format" hint="Free-text hint for now — the preview always shows a locale-aware month and year.">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              required
              className={inputStyles.input}
              value={values.dateFormat}
              onChange={(event) => onChange({ ...values, dateFormat: event.target.value })}
            />
          )}
        </Field>
        <Field label="Page limit">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={1}
              required
              className={inputStyles.input}
              value={values.pageLimit}
              onChange={(event) => onChange({ ...values, pageLimit: Number(event.target.value) })}
            />
          )}
        </Field>
      </div>

      {/* Only reachable for a presentation saved before this template restriction, or with a
          hand-edited TemplateKey — the disabled select above can't offer a way off an unsupported
          value, so this is the correction path. It only edits local form state; nothing is
          persisted until the user clicks Save/Create. */}
      {values.templateKey !== SUPPORTED_TEMPLATE_KEY && (
        <div className={layout.correction}>
          <p role="alert" className={fieldStyles.error}>
            This presentation uses "{values.templateKey}", which export doesn't support.
          </p>
          <Button type="button" variant="secondary" onClick={() => onChange({ ...values, templateKey: SUPPORTED_TEMPLATE_KEY })}>
            Use the default template
          </Button>
        </div>
      )}

      <Field label="Summary override" hint="Optional — replaces your profile's summary just for this presentation.">
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            className={inputStyles.input}
            value={values.summaryOverrideMarkdown ?? ''}
            onChange={(event) => onChange({ ...values, summaryOverrideMarkdown: event.target.value || null })}
          />
        )}
      </Field>

      <div className={layout.checkboxRow}>
        <label className={layout.checkbox}>
          {/* Photo export isn't implemented (no upload/storage path exists yet) — the box can only be
              unchecked here, never checked, so a new presentation can never enable it; export itself
              also rejects IncludePhoto=true explicitly as a second line of defence. */}
          <input
            type="checkbox"
            checked={values.includePhoto}
            disabled={!values.includePhoto}
            onChange={(event) => onChange({ ...values, includePhoto: event.target.checked })}
          />
          Include photo
        </label>
        <label className={layout.checkbox}>
          <input type="checkbox" checked={values.includeEmail} onChange={(event) => onChange({ ...values, includeEmail: event.target.checked })} />
          Include email
        </label>
        <label className={layout.checkbox}>
          <input type="checkbox" checked={values.includePhone} onChange={(event) => onChange({ ...values, includePhone: event.target.checked })} />
          Include phone
        </label>
        <label className={layout.checkbox}>
          <input type="checkbox" checked={values.includeAddress} onChange={(event) => onChange({ ...values, includeAddress: event.target.checked })} />
          Include address
        </label>
      </div>
      {values.includePhoto ? (
        <p className={fieldStyles.hint}>Photo export isn't supported yet — exporting with this enabled will be rejected. You can uncheck it here.</p>
      ) : (
        <p className={fieldStyles.hint}>Photo export isn't supported yet, so this can't be enabled.</p>
      )}
    </div>
  )
}

type CreateCVPresentationFormProps = {
  mode: 'create'
  onCreated: (id: string) => void
  onCancel: () => void
  onGoToProfile: () => void
}

type EditCVPresentationFormProps = {
  mode: 'edit'
  presentation: CVPresentationResponse
  onSaved: () => void
  onCancel: () => void
}

type CVPresentationFormProps = CreateCVPresentationFormProps | EditCVPresentationFormProps

type ProfileLoadState = 'loading' | 'ready' | 'no-profile' | 'error'

export function CVPresentationForm(props: CVPresentationFormProps) {
  const isCreateMode = props.mode === 'create'
  const [values, setValues] = useState<FormValues>(props.mode === 'edit' ? fromPresentation(props.presentation) : DEFAULT_VALUES)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [profileLoadState, setProfileLoadState] = useState<ProfileLoadState>(isCreateMode ? 'loading' : 'ready')
  const [professionalProfileId, setProfessionalProfileId] = useState<string | null>(null)

  useEffect(() => {
    if (!isCreateMode) {
      return
    }

    fetchProfessionalProfile()
      .then((profile) => {
        if (!profile) {
          setProfileLoadState('no-profile')
          return
        }

        setProfessionalProfileId(profile.id)
        setProfileLoadState('ready')
      })
      .catch(() => {
        setProfileLoadState('error')
      })
  }, [isCreateMode])

  const handleSubmit = async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      if (props.mode === 'create') {
        if (!professionalProfileId) {
          return
        }

        const id = await createCVPresentation({ ...values, professionalProfileId, pageLimit: toProfileNumber(values.pageLimit) })
        if (!id) {
          setError('Could not create this CV presentation — your professional profile could not be referenced.')
          return
        }

        props.onCreated(id)
      } else {
        await updateCVPresentation(props.presentation.id, values)
        props.onSaved()
      }
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong saving this CV presentation.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  if (props.mode === 'create' && profileLoadState === 'loading') {
    return (
      <p role="status">Loading…</p>
    )
  }

  if (props.mode === 'create' && profileLoadState === 'no-profile') {
    return (
      <div className={layout.stack}>
        <p>You need a professional profile before you can create a CV presentation.</p>
        <Button variant="primary" onClick={props.onGoToProfile}>
          Go to your professional profile
        </Button>
      </div>
    )
  }

  if (props.mode === 'create' && profileLoadState === 'error') {
    return <p role="alert">Something went wrong loading your professional profile.</p>
  }

  return (
    <form
      className={layout.stack}
      onSubmit={(event) => {
        event.preventDefault()
        void handleSubmit()
      }}
    >
      <CVPresentationFormFields values={values} onChange={setValues} />

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
