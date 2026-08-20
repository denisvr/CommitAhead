import { useState } from 'react'

// The save/error lifecycle shared by every ProfessionalProfile child collection — each has its
// own PUT-whole-collection endpoint (no per-entry create/delete routes). Entries live in the
// PARENT's state (ProfessionalProfilePage), not here: the section calls `onChange` on every edit
// so the profile preview reflects what is being typed, not only what was last saved, and this
// hook only owns the save lifecycle (triggered by Done/Delete/Add, not a separate Save button)
// against whatever the parent currently holds.
export function useSectionSave<T>(entries: T[], save: (entries: T[]) => Promise<void>) {
  const [error, setError] = useState<string | null>(null)

  // Accepts an explicit array for callers that persist immediately after computing a new array
  // (e.g. a delete) rather than after `onChange` has already round-tripped through the parent and
  // back into this hook's own `entries` closure — passing it directly sidesteps that render lag.
  const handleSave = async (overrideEntries?: T[]) => {
    setError(null)
    try {
      await save(overrideEntries ?? entries)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong saving this section.')
    }
  }

  return { error, handleSave }
}
