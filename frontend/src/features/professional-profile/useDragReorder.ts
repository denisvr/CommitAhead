import { useState, type DragEvent } from 'react'
import type { CollapsibleRowReorder } from '../../design-system/components/CollapsibleRow'

// Native HTML5 drag-and-drop reordering shared by Experience/Education/Certifications/Projects —
// see CollapsibleRow's own header comment for why native drag events rather than a library.
// `persist` is called with the reordered array immediately (no separate Save button anywhere in
// this app any more — see useSectionSave's own comment on why every mutating action saves as it
// happens). `disabled` (typically the section's own `isSaving`) blocks starting a new drag while
// a previous reorder/edit for the same section is still being persisted.
export function useDragReorder<T extends { id: string }>(
  items: T[],
  onChange: (items: T[]) => void,
  persist: (items: T[]) => void,
  disabled = false,
) {
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [dropTargetId, setDropTargetId] = useState<string | null>(null)

  const reorderFor = (id: string, label: string): CollapsibleRowReorder => ({
    label,
    isDragging: draggingId === id,
    isDropTarget: dropTargetId === id,
    onHandleDragStart: (event: DragEvent<HTMLSpanElement>) => {
      if (disabled) {
        event.preventDefault()
        return
      }
      setDraggingId(id)
      event.dataTransfer.effectAllowed = 'move'
      event.dataTransfer.setData('text/plain', id)
    },
    onDragEnd: () => {
      setDraggingId(null)
      setDropTargetId(null)
    },
    onRowDragOver: () => {
      if (draggingId && draggingId !== id) setDropTargetId(id)
    },
    onRowDrop: (event: DragEvent<HTMLElement>) => {
      event.preventDefault()
      const sourceId = draggingId
      setDraggingId(null)
      setDropTargetId(null)
      if (!sourceId || sourceId === id) return

      const sourceIndex = items.findIndex((item) => item.id === sourceId)
      const targetIndex = items.findIndex((item) => item.id === id)
      if (sourceIndex < 0 || targetIndex < 0) return

      const next = [...items]
      const [moved] = next.splice(sourceIndex, 1)
      next.splice(targetIndex, 0, moved)
      onChange(next)
      persist(next)
    },
  })

  return { reorderFor }
}
