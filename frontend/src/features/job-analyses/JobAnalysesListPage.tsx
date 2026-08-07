import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Chip } from '../../design-system/components/Chip'
import { EmptyState } from '../../design-system/components/EmptyState'
import { Icon } from '../../design-system/Icon'
import { fetchJobAnalyses, type JobAnalysisResponse } from './api'
import styles from './JobAnalysesListPage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

type JobAnalysesListPageProps = {
  onSelectAnalysis: (id: string) => void
  onCreateNew: () => void
}

function sourceLabel(analysis: JobAnalysisResponse): string {
  return analysis.jobSource.kind === 'UploadedFile' ? 'Uploaded PDF' : 'Pasted text'
}

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// components.md AppShell destination 4, "Job analyses" — a flat list, unlike Study Items: a
// JobAnalysis has no Active/Archived status to filter by.
export function JobAnalysesListPage({ onSelectAnalysis, onCreateNew }: JobAnalysesListPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [analyses, setAnalyses] = useState<JobAnalysisResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [retryToken, setRetryToken] = useState(0)

  useEffect(() => {
    let stale = false

    fetchJobAnalyses()
      .then((data) => {
        if (stale) return
        setAnalyses(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        if (stale) return
        setError(caught instanceof Error ? caught.message : 'Something went wrong loading your job analyses.')
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
        <h1 className={styles.title}>Job analyses</h1>
        <Button variant="primary" onClick={onCreateNew}>
          New job analysis
        </Button>
      </header>

      {loadState === 'loading' && (
        <p className={styles.status} role="status">
          Loading your job analyses…
        </p>
      )}

      {loadState === 'error' && (
        <>
          <p role="alert">{error}</p>
          <Button onClick={retry}>Try again</Button>
        </>
      )}

      {loadState === 'ready' &&
        (analyses.length === 0 ? (
          <EmptyState
            title="No job analyses yet"
            description="Paste a job posting or upload the PDF to start identifying requirements and gaps."
            action={{ label: 'New job analysis', onClick: onCreateNew }}
          />
        ) : (
          <ul className={styles.list}>
            {analyses.map((analysis) => (
              <li key={analysis.id} className={styles.row}>
                <button type="button" className={styles.link} onClick={() => onSelectAnalysis(analysis.id)}>
                  <span className={styles.main}>
                    <span className={styles.title2}>{analysis.title}</span>
                    <Chip>{sourceLabel(analysis)}</Chip>
                  </span>
                  <span className={styles.updated}>Updated {formatDate(analysis.updatedAtUtc)}</span>
                  <Icon name="chevron-right" className={styles.chevron} />
                </button>
              </li>
            ))}
          </ul>
        ))}
    </div>
  )
}
