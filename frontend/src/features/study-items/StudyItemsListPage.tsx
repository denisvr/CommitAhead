import { useCallback, useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { EmptyState } from '../../design-system/components/EmptyState'
import { StudyItemRow } from '../../design-system/components/StudyItemRow'
import { Tabs } from '../../design-system/components/Tabs'
import { fetchStudyItems, type StudyItemStatus, type StudyItemSummaryResponse } from './api'
import styles from './StudyItemsListPage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

type StudyItemsListPageProps = {
  onSelectItem: (id: string) => void
  onCreateNew: () => void
}

const TABS = [
  { key: 'Active', label: 'Active' },
  { key: 'Archived', label: 'Archived' },
]

// components.md AppShell destination 2 — every StudyItem regardless of rank, unlike the ranked
// Study Queue which only ever shows Active items. Archival/restoration are user-initiated here;
// nothing in this page ever changes an item's status on its own.
export function StudyItemsListPage({ onSelectItem, onCreateNew }: StudyItemsListPageProps) {
  const [activeTab, setActiveTab] = useState<StudyItemStatus>('Active')
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [items, setItems] = useState<StudyItemSummaryResponse[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async (status: StudyItemStatus) => {
    try {
      setItems(await fetchStudyItems(status))
      setLoadState('ready')
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong loading your study items.')
      setLoadState('error')
    }
  }, [])

  // Inlined rather than calling load() directly — see StudyQueuePage for why (linter's
  // set-state-in-effect rule treats any call to a state-setting function reference as
  // synchronous, regardless of the await inside it). Does not reset loadState to 'loading' on a
  // tab switch for the same reason — the previous tab's items stay visible until the new fetch
  // resolves, then swap in; only the initial mount shows the "Loading…" state.
  //
  // The `stale` flag guards against a slower, earlier fetch (e.g. for a tab the user has since
  // switched away from) resolving after a faster, later one and overwriting the currently
  // selected tab's just-applied data with a response for a tab that isn't showing anymore.
  useEffect(() => {
    let stale = false

    fetchStudyItems(activeTab)
      .then((data) => {
        if (stale) return
        setItems(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        if (stale) return
        setError(caught instanceof Error ? caught.message : 'Something went wrong loading your study items.')
        setLoadState('error')
      })

    return () => {
      stale = true
    }
  }, [activeTab])

  const retry = () => void load(activeTab)

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Study items</h1>
        <Button variant="primary" onClick={onCreateNew}>
          New study item
        </Button>
      </header>

      <Tabs tabs={TABS} activeTab={activeTab} onChange={(key) => setActiveTab(key as StudyItemStatus)} aria-label="Filter by status" />

      <div id={`tabpanel-${activeTab}`} role="tabpanel" aria-labelledby={`tab-${activeTab}`}>
        {loadState === 'loading' && (
          <p className={styles.status} role="status">
            Loading your study items…
          </p>
        )}

        {loadState === 'error' && (
          <>
            <p role="alert">{error}</p>
            <Button onClick={retry}>Try again</Button>
          </>
        )}

        {loadState === 'ready' &&
          (items.length === 0 ? (
            <EmptyState
              title={activeTab === 'Active' ? 'No active study items yet' : 'No archived study items'}
              description={
                activeTab === 'Active'
                  ? 'Add your first study item — a LeetCode problem, a system design prompt, a behavioral story, or a theory topic.'
                  : 'Items you archive stay here, with their history intact, until you restore or delete them.'
              }
              action={activeTab === 'Active' ? { label: 'New study item', onClick: onCreateNew } : undefined}
            />
          ) : (
            <ul className={styles.list}>
              {items.map((item) => (
                <StudyItemRow key={item.id} item={item} onSelect={onSelectItem} />
              ))}
            </ul>
          ))}
      </div>
    </div>
  )
}
