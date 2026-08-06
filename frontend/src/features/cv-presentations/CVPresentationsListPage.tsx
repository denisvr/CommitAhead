import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { EmptyState } from '../../design-system/components/EmptyState'
import { Icon } from '../../design-system/Icon'
import { fetchCVPresentations, type CVPresentationResponse } from './api'
import styles from './CVPresentationsListPage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

type CVPresentationsListPageProps = {
  onSelectPresentation: (id: string) => void
  onCreateNew: () => void
}

export function CVPresentationsListPage({ onSelectPresentation, onCreateNew }: CVPresentationsListPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [presentations, setPresentations] = useState<CVPresentationResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [retryToken, setRetryToken] = useState(0)

  useEffect(() => {
    let stale = false

    fetchCVPresentations()
      .then((data) => {
        if (stale) return
        setPresentations(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        if (stale) return
        setError(caught instanceof Error ? caught.message : 'Something went wrong loading your CV presentations.')
        setLoadState('error')
      })

    return () => {
      stale = true
    }
  }, [retryToken])

  const retry = () => setRetryToken((token) => token + 1)

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>CV presentations</h1>
        <Button variant="primary" onClick={onCreateNew}>
          New CV presentation
        </Button>
      </header>

      {loadState === 'loading' && (
        <p className={styles.status} role="status">
          Loading your CV presentations…
        </p>
      )}

      {loadState === 'error' && (
        <>
          <p role="alert">{error}</p>
          <Button onClick={retry}>Try again</Button>
        </>
      )}

      {loadState === 'ready' &&
        (presentations.length === 0 ? (
          <EmptyState
            title="No CV presentations yet"
            description="A CV presentation curates and orders entries from your professional profile for a specific market or role — without duplicating them."
            action={{ label: 'New CV presentation', onClick: onCreateNew }}
          />
        ) : (
          <ul className={styles.list}>
            {presentations.map((presentation) => (
              <li key={presentation.id} className={styles.row}>
                <button type="button" className={styles.link} onClick={() => onSelectPresentation(presentation.id)}>
                  <span className={styles.main}>
                    <span className={styles.rowLabel}>{presentation.label}</span>
                    <span className={styles.rowMeta}>
                      {presentation.targetMarket}
                      {presentation.targetRole ? ` · ${presentation.targetRole}` : ''}
                    </span>
                  </span>
                  <Icon name="chevron-right" className={styles.chevron} />
                </button>
              </li>
            ))}
          </ul>
        ))}
    </div>
  )
}
