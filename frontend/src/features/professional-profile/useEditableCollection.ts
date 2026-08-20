import { useRef, useState } from 'react'

// The save/error lifecycle shared by every ProfessionalProfile child collection — each has its
// own PUT-whole-collection endpoint (no per-entry create/delete routes). Entries live in the
// PARENT's state (ProfessionalProfilePage), not here: the section calls `onChange` on every edit
// so the profile preview reflects what is being typed, not only what was last saved, and this
// hook only owns the save lifecycle (triggered by Done/Delete/Add/reorder, not a separate Save
// button) against whatever the parent currently holds.
export function useSectionSave<T>(entries: T[], save: (entries: T[]) => Promise<void>) {
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  // Every call chains after whichever one is already in flight, so two overlapping PUTs (e.g. a
  // Delete fired right before Done) reach the whole-collection endpoint in call order and settle
  // in that same order — without this, the network could resolve them out of order and let a
  // stale collection silently overwrite a newer one.
  const queueRef = useRef<Promise<void>>(Promise.resolve())

  // Accepts an explicit array for callers that persist immediately after computing a new array
  // (e.g. a delete) rather than after `onChange` has already round-tripped through the parent and
  // back into this hook's own `entries` closure — passing it directly sidesteps that render lag.
  // Returns whether the save succeeded, so a caller can decide what to do next (close edit mode,
  // revert an optimistic update) instead of assuming success.
  const handleSave = (overrideEntries?: T[]): Promise<boolean> => {
    const toSave = overrideEntries ?? entries
    const run = async (): Promise<boolean> => {
      setIsSaving(true)
      setError(null)
      try {
        await save(toSave)
        return true
      } catch (caught) {
        setError(caught instanceof Error ? caught.message : 'Something went wrong saving this section.')
        return false
      } finally {
        setIsSaving(false)
      }
    }
    const settled = queueRef.current.then(run, run)
    queueRef.current = settled.then(
      () => undefined,
      () => undefined,
    )
    return settled
  }

  return { error, isSaving, handleSave }
}
