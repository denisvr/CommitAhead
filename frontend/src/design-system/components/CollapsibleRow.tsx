import { useState, type DragEvent, type ReactNode } from 'react'
import { Icon } from '../Icon'
import styles from './CollapsibleRow.module.css'

// A caller passes this to enable native HTML5 drag-and-drop reordering on this row — see this
// file's own header comment for why native drag events, not a library. Move up/down buttons
// (already in every reorderable row's body) remain the keyboard/no-mouse path; this is a mouse-
// only addition, not a replacement.
export type CollapsibleRowReorder = {
  label: string
  isDragging: boolean
  isDropTarget: boolean
  onHandleDragStart: (event: DragEvent<HTMLSpanElement>) => void
  onDragEnd: () => void
  onRowDragOver: (event: DragEvent<HTMLElement>) => void
  onRowDrop: (event: DragEvent<HTMLElement>) => void
}

type CollapsibleRowProps = {
  id?: string
  ordinal?: number
  title: ReactNode
  subtitle?: ReactNode
  meta?: ReactNode
  status?: ReactNode
  defaultOpen?: boolean
  // Controlled pair, for a caller that needs to open a specific row from outside (the profile
  // preview opening the experience it was clicked from). Omit both to stay uncontrolled.
  open?: boolean
  onToggle?: () => void
  reorder?: CollapsibleRowReorder
  children: ReactNode
}

// A repeated entry inside a Card (components.md "CollapsibleRow") — one position, one
// qualification, one certification. The header is a real <button> carrying aria-expanded; the
// open state is an accent border/background, never a plain fill change. Reordering has two
// parallel paths: Move up/down buttons in the row body (accessible, keyboard-first — see
// components.md's own SelectionOrderEditor precedent) and, when a caller passes `reorder`, a
// leading drag handle for native HTML5 drag-and-drop (mouse/trackpad only — the HTML5 Drag and
// Drop API has no keyboard path of its own, which is exactly why the buttons stay the primary
// mechanism rather than being replaced). Not dnd-kit or any other library: this app's CSP is
// `style-src 'self'` with no `unsafe-inline` (docs/security/threat-model.md), and every JS drag
// library's live "item follows the cursor" feedback works by writing an inline transform style —
// blocked outright under this CSP. Native drag's own ghost image is rendered by the browser
// itself, outside the page's style pipeline entirely, so it isn't affected.
export function CollapsibleRow({ id, ordinal, title, subtitle, meta, status, defaultOpen = false, open: controlledOpen, onToggle, reorder, children }: CollapsibleRowProps) {
  const [internalOpen, setInternalOpen] = useState(defaultOpen)
  const isControlled = controlledOpen !== undefined
  const open = isControlled ? controlledOpen : internalOpen
  const toggle = () => (isControlled ? onToggle?.() : setInternalOpen((value) => !value))

  const rowClassName = [styles.row, open ? styles.open : '', reorder?.isDragging ? styles.dragging : '', reorder?.isDropTarget ? styles.dropTarget : '']
    .filter(Boolean)
    .join(' ')

  return (
    <article
      id={id}
      className={rowClassName}
      onDragOver={
        reorder
          ? (event) => {
              event.preventDefault()
              reorder.onRowDragOver(event)
            }
          : undefined
      }
      onDrop={reorder?.onRowDrop}
    >
      <div className={styles.headRow}>
        {reorder && (
          <span
            className={styles.dragHandle}
            draggable
            onDragStart={reorder.onHandleDragStart}
            onDragEnd={reorder.onDragEnd}
            aria-hidden="true"
            title={`Drag to reorder ${reorder.label}`}
          >
            <Icon name="grip-vertical" />
          </span>
        )}
        <button type="button" className={styles.head} aria-expanded={open} onClick={toggle}>
          {ordinal !== undefined && <span className={styles.ordinal}>{ordinal}</span>}
          <span className={styles.titleBlock}>
            <span className={styles.title}>{title}</span>
            {subtitle && <span className={styles.subtitle}>{subtitle}</span>}
            {meta && <span className={styles.meta}>{meta}</span>}
          </span>
          <span className={styles.side}>
            {status}
            <span className={styles.chevron}>
              <Icon name="chevron-down" />
            </span>
          </span>
        </button>
      </div>
      {open && <div className={[styles.body, reorder ? styles.bodyIndented : ''].filter(Boolean).join(' ')}>{children}</div>}
    </article>
  )
}
