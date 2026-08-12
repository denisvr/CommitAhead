// Individual local icons rendered as inline JSX, per docs/design/design-system/readme.md — never
// a runtime-injected sprite. Paths copied from the approved Lucide-derived source SVGs in
// docs/design/design-system/assets/icons/. Add a name only when a component in this slice needs it.
export type IconName = 'plus' | 'x' | 'trash-2' | 'chevron-right' | 'chevron-up' | 'chevron-down' | 'pencil' | 'download'

const paths: Record<IconName, string[]> = {
  plus: ['M5 12h14', 'M12 5v14'],
  x: ['M18 6 6 18', 'M6 6 12 12'],
  'trash-2': [
    'M10 11v6',
    'M14 11v6',
    'M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6',
    'M3 6h18',
    'M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2',
  ],
  'chevron-right': ['m9 18 6-6-6-6'],
  'chevron-up': ['m18 15-6-6-6 6'],
  'chevron-down': ['m6 9 6 6 6-6'],
  pencil: ['M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z', 'm15 5 4 4'],
  download: ['M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4', 'M7 10 12 15 17 10', 'M12 15 12 3'],
}

export function Icon({ name, className }: { name: IconName; className?: string }) {
  return (
    <svg
      className={className}
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.75}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {paths[name].map((d) => (
        <path key={d} d={d} />
      ))}
    </svg>
  )
}
