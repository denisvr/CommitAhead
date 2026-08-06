import { useId } from 'react'
import { Button } from './Button'
import { Icon } from '../Icon'
import styles from './SelectionOrderEditor.module.css'

type SelectionOrderEditorProps<T> = {
  candidates: T[]
  selectedIds: string[]
  onChange: (selectedIds: string[]) => void
  getId: (candidate: T) => string
  getLabel: (candidate: T) => string
  addLabel: string
  emptyLabel: string
  disabled?: boolean
}

// CVPresentation's seven selection collections are ordered by array position, not a documented
// drag-and-reorder interaction (docs/design/design-system/page-patterns.md has no such pattern,
// and no DnD dependency exists in this project) — move-up/move-down buttons are the accessible,
// dependency-free choice. A dangling selected id (one that no longer resolves against
// `candidates` — invariant 25's cleanup runs server-side, but a stale in-flight edit could still
// race it locally) is skipped rather than crashing.
//
// `disabled` (e.g. while a caller's own save for this same selection is still in flight) disables
// every control here — the caller is responsible for not calling onChange again until it clears,
// this component just reflects that state visually so there's nothing to click mid-save.
export function SelectionOrderEditor<T>({ candidates, selectedIds, onChange, getId, getLabel, addLabel, emptyLabel, disabled = false }: SelectionOrderEditorProps<T>) {
  const selectId = useId()
  const candidatesById = new Map(candidates.map((candidate) => [getId(candidate), candidate]))
  const selectedCandidates = selectedIds.map((id) => candidatesById.get(id)).filter((candidate): candidate is T => candidate !== undefined)
  const availableCandidates = candidates.filter((candidate) => !selectedIds.includes(getId(candidate)))

  const moveTo = (index: number, delta: number) => {
    const next = [...selectedIds]
    const target = index + delta
    if (target < 0 || target >= next.length) return
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
  }

  const remove = (id: string) => onChange(selectedIds.filter((selectedId) => selectedId !== id))

  const add = (id: string) => {
    if (id) onChange([...selectedIds, id])
  }

  return (
    <div className={styles.wrapper}>
      {selectedCandidates.length === 0 ? (
        <p className={styles.empty}>{emptyLabel}</p>
      ) : (
        <ol className={styles.selectedList}>
          {selectedCandidates.map((candidate, index) => {
            const id = getId(candidate)
            const label = getLabel(candidate)
            return (
              <li key={id} className={styles.selectedRow}>
                <span className={styles.selectedLabel}>{label}</span>
                <div className={styles.rowActions}>
                  <Button type="button" variant="ghost" onClick={() => moveTo(index, -1)} disabled={disabled || index === 0} aria-label={`Move ${label} up`}>
                    <Icon name="chevron-up" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => moveTo(index, 1)}
                    disabled={disabled || index === selectedCandidates.length - 1}
                    aria-label={`Move ${label} down`}
                  >
                    <Icon name="chevron-down" />
                  </Button>
                  <Button type="button" variant="ghost" onClick={() => remove(id)} disabled={disabled}>
                    Remove
                  </Button>
                </div>
              </li>
            )
          })}
        </ol>
      )}

      {availableCandidates.length > 0 && (
        <div className={styles.addRow}>
          <label htmlFor={selectId} className={styles.addLabel}>
            {addLabel}
          </label>
          <select
            id={selectId}
            className={styles.select}
            value=""
            disabled={disabled}
            onChange={(event) => {
              add(event.target.value)
              event.target.value = ''
            }}
          >
            <option value="" disabled>
              Choose…
            </option>
            {availableCandidates.map((candidate) => (
              <option key={getId(candidate)} value={getId(candidate)}>
                {getLabel(candidate)}
              </option>
            ))}
          </select>
        </div>
      )}
    </div>
  )
}
