import styles from './Brand.module.css'

const MARK_PATH = 'M2 0h28a2 2 0 0 1 2 2v44l-9.6-11.4h-4L0 46V2a2 2 0 0 1 2-2Z M6 11.5h20v3.5H6z'

type BookmarkMarkProps = {
  className?: string
  size?: number
}

// The Bookmark symbol alone (components.md "Brand"), for places that need only the icon —
// AuthScreen pairs it with a real <h1> instead of the full lockup below, so the SVG's own
// role="img"/aria-label never doubles up with a heading's accessible name.
export function BookmarkMark({ className, size = 16 }: BookmarkMarkProps) {
  return (
    <svg
      className={[styles.mark, className].filter(Boolean).join(' ')}
      viewBox="0 0 32 46"
      width={size}
      height={(size * 46) / 32}
      fill="currentColor"
      fillRule="evenodd"
      role="img"
      aria-label="CommitAhead"
    >
      <path d={MARK_PATH} />
    </svg>
  )
}

type BrandProps = {
  className?: string
  size?: 'sm' | 'lg'
}

// The full mark + wordmark lockup (components.md "Brand"). Not a click target here — AppShell
// places it inside the sidebar/mobile header; neither gives it a navigation destination yet.
export function Brand({ className, size = 'sm' }: BrandProps) {
  return (
    <div className={[styles.brand, size === 'lg' ? styles.large : '', className].filter(Boolean).join(' ')}>
      <BookmarkMark size={size === 'lg' ? 28 : 16} />
      <span className={styles.wordmark}>
        <span className={styles.wordmarkStrong}>Commit</span>
        <span className={styles.wordmarkLight}>Ahead</span>
      </span>
    </div>
  )
}
