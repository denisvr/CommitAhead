import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import inputStyles from '../../design-system/components/Input.module.css'
import { fetchScoringConfig, resetScoringConfig, toNumber, updateScoringConfig, type ScoringConfigResponse } from '../study-items/api'
import layout from '../study-items/FormLayout.module.css'
import styles from './ScoringSettingsPage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

const REQUIRED_TOTAL = 100

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

// page-patterns.md Phase 1 delivery order includes "scoring settings" — the effective weights an
// owner can override (default 40/35/25, ADR-0003), reachable from the AppShell "Settings"
// destination (components.md). The queue itself needs no explicit refresh after a change: it
// re-fetches on every mount, and navigating here unmounts it.
export function ScoringSettingsPage() {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [loadError, setLoadError] = useState<string | null>(null)
  const [isOverridden, setIsOverridden] = useState(false)
  const [importanceWeight, setImportanceWeight] = useState(0)
  const [demandWeight, setDemandWeight] = useState(0)
  const [masteryGapWeight, setMasteryGapWeight] = useState(0)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isResetting, setIsResetting] = useState(false)

  const applyConfig = (config: ScoringConfigResponse) => {
    setImportanceWeight(toNumber(config.importanceWeight))
    setDemandWeight(toNumber(config.demandWeight))
    setMasteryGapWeight(toNumber(config.masteryGapWeight))
    setIsOverridden(config.isOverridden)
  }

  const load = useCallback(async () => {
    try {
      applyConfig(await fetchScoringConfig())
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading scoring settings.'))
      setLoadState('error')
    }
  }, [])

  // Inlined rather than calling load() directly — see the study-items pages for why (the
  // set-state-in-effect lint rule treats any call to a state-setting function reference as
  // synchronous, regardless of the await inside it).
  useEffect(() => {
    fetchScoringConfig()
      .then((config) => {
        applyConfig(config)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading scoring settings.'))
        setLoadState('error')
      })
  }, [])

  const retry = () => {
    setLoadState('loading')
    void load()
  }

  if (loadState === 'loading') {
    return (
      <p className={styles.status} role="status">
        Loading scoring settings…
      </p>
    )
  }

  if (loadState === 'error') {
    return (
      <div className={styles.page}>
        <p role="alert">{loadError}</p>
        <Button onClick={retry}>Try again</Button>
      </div>
    )
  }

  const total = importanceWeight + demandWeight + masteryGapWeight

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setValidationError(null)
    setActionError(null)

    if ([importanceWeight, demandWeight, masteryGapWeight].some((weight) => !Number.isInteger(weight) || weight < 0)) {
      setValidationError('Each weight must be a non-negative whole number.')
      return
    }

    if (total !== REQUIRED_TOTAL) {
      setValidationError(`Weights must sum to exactly ${REQUIRED_TOTAL} (currently ${total}).`)
      return
    }

    setIsSaving(true)
    try {
      await updateScoringConfig(importanceWeight, demandWeight, masteryGapWeight)
      applyConfig(await fetchScoringConfig())
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong saving scoring settings.'))
    } finally {
      setIsSaving(false)
    }
  }

  const handleReset = async () => {
    setActionError(null)
    setValidationError(null)
    setIsResetting(true)
    try {
      await resetScoringConfig()
      applyConfig(await fetchScoringConfig())
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong resetting scoring settings.'))
    } finally {
      setIsResetting(false)
    }
  }

  return (
    <form className={[styles.page, layout.stack].join(' ')} onSubmit={handleSubmit}>
      <header className={styles.header}>
        <h1 className={styles.title}>Scoring settings</h1>
        <p className={styles.overrideStatus}>{isOverridden ? 'Using custom weights' : 'Using default weights (40 / 35 / 25)'}</p>
      </header>

      <div className={layout.row}>
        <Field label="Importance weight">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={0}
              step={1}
              className={inputStyles.input}
              value={importanceWeight}
              onChange={(event) => setImportanceWeight(Number(event.target.value))}
            />
          )}
        </Field>
        <Field label="Demand weight">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={0}
              step={1}
              className={inputStyles.input}
              value={demandWeight}
              onChange={(event) => setDemandWeight(Number(event.target.value))}
            />
          )}
        </Field>
        <Field label="Mastery-gap weight">
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="number"
              min={0}
              step={1}
              className={inputStyles.input}
              value={masteryGapWeight}
              onChange={(event) => setMasteryGapWeight(Number(event.target.value))}
            />
          )}
        </Field>
      </div>

      <p className={styles.total}>
        Total: {total} / {REQUIRED_TOTAL}
      </p>

      {validationError && <p role="alert">{validationError}</p>}
      {actionError && <p role="alert">{actionError}</p>}

      <div className={styles.actions}>
        <Button type="submit" variant="primary" isLoading={isSaving}>
          Save
        </Button>
        <Button type="button" variant="ghost" onClick={handleReset} isLoading={isResetting}>
          Reset to defaults
        </Button>
      </div>
    </form>
  )
}
