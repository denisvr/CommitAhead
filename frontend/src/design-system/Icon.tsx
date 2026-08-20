// Individual local icons rendered as inline JSX, per docs/design/design-system/readme.md — never
// a runtime-injected sprite. Shapes copied from the approved Lucide-derived source SVGs in
// docs/design/design-system/assets/icons/. Add a name only when a component in this slice needs it.
type IconShape =
  | { kind: 'path'; d: string }
  | { kind: 'circle'; cx: number; cy: number; r: number }
  | { kind: 'ellipse'; cx: number; cy: number; rx: number; ry: number }
  | { kind: 'line'; x1: number; y1: number; x2: number; y2: number }
  | { kind: 'rect'; x: number; y: number; width: number; height: number; rx?: number }

export type IconName =
  | 'plus'
  | 'x'
  | 'trash-2'
  | 'chevron-right'
  | 'chevron-up'
  | 'chevron-down'
  | 'pencil'
  | 'download'
  | 'sun'
  | 'moon'
  | 'monitor'
  | 'user-round'
  | 'briefcase'
  | 'graduation-cap'
  | 'wrench'
  | 'languages'
  | 'award'
  | 'rocket'
  | 'link'
  | 'circle-alert'
  | 'database'
  | 'file-text'
  | 'chevrons-left'
  | 'log-out'
  | 'house'
  | 'check'
  | 'grip-vertical'

const path = (d: string): IconShape => ({ kind: 'path', d })

const shapes: Record<IconName, IconShape[]> = {
  plus: [path('M5 12h14'), path('M12 5v14')],
  x: [path('M18 6 6 18'), path('M6 6 18 18')],
  'trash-2': [
    path('M10 11v6'),
    path('M14 11v6'),
    path('M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6'),
    path('M3 6h18'),
    path('M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2'),
  ],
  'chevron-right': [path('m9 18 6-6-6-6')],
  'chevron-up': [path('m18 15-6-6-6 6')],
  'chevron-down': [path('m6 9 6 6 6-6')],
  pencil: [path('M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z'), path('m15 5 4 4')],
  download: [path('M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'), path('M7 10 12 15 17 10'), path('M12 15 12 3')],
  sun: [
    path('M12 2v2'),
    path('M12 20v2'),
    path('m4.93 4.93 1.41 1.41'),
    path('m17.66 17.66 1.41 1.41'),
    path('M2 12h2'),
    path('M20 12h2'),
    path('m6.34 17.66-1.41 1.41'),
    path('m19.07 4.93-1.41 1.41'),
    { kind: 'circle', cx: 12, cy: 12, r: 4 },
  ],
  moon: [path('M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401')],
  monitor: [{ kind: 'rect', x: 2, y: 3, width: 20, height: 14, rx: 2 }, path('M8 21h8'), path('M12 17v4')],
  'user-round': [{ kind: 'circle', cx: 12, cy: 8, r: 5 }, path('M20 21a8 8 0 0 0-16 0')],
  briefcase: [path('M16 20V4a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16'), { kind: 'rect', x: 2, y: 6, width: 20, height: 14, rx: 2 }],
  'graduation-cap': [
    path('M21.42 10.922a1 1 0 0 0-.019-1.838L12.83 5.18a2 2 0 0 0-1.66 0L2.6 9.08a1 1 0 0 0 0 1.832l8.57 3.908a2 2 0 0 0 1.66 0z'),
    path('M22 10v6'),
    path('M6 12.5V16a6 3 0 0 0 12 0v-3.5'),
  ],
  wrench: [path('M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z')],
  languages: [path('m5 8 6 6'), path('m4 14 6-6 2-3'), path('M2 5h12'), path('M7 2h1'), path('m22 22-5-10-5 10'), path('M14 18h6')],
  award: [
    path('m15.477 12.89 1.515 8.526a.5.5 0 0 1-.81.47l-3.58-2.687a1 1 0 0 0-1.197 0l-3.586 2.686a.5.5 0 0 1-.81-.469l1.514-8.526'),
    { kind: 'circle', cx: 12, cy: 8, r: 6 },
  ],
  rocket: [
    path('M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91 0z'),
    path('m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z'),
    path('M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0'),
    path('M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5'),
  ],
  link: [path('M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71'), path('M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71')],
  'circle-alert': [{ kind: 'circle', cx: 12, cy: 12, r: 10 }, { kind: 'line', x1: 12, y1: 8, x2: 12, y2: 12 }, { kind: 'line', x1: 12, y1: 16, x2: 12.01, y2: 16 }],
  database: [
    { kind: 'ellipse', cx: 12, cy: 5, rx: 9, ry: 3 },
    path('M3 5v14a9 3 0 0 0 18 0V5'),
    path('M3 12a9 3 0 0 0 18 0'),
  ],
  'file-text': [
    path('M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z'),
    path('M14 2v4a2 2 0 0 0 2 2h4'),
    path('M10 9H8'),
    path('M16 13H8'),
    path('M16 17H8'),
  ],
  'chevrons-left': [path('m11 17-5-5 5-5'), path('m18 17-5-5 5-5')],
  'log-out': [path('M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'), path('M16 17 21 12 16 7'), { kind: 'line', x1: 21, y1: 12, x2: 9, y2: 12 }],
  house: [
    path('M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8'),
    path('M3 10a2 2 0 0 1 .709-1.528l7-6a2 2 0 0 1 2.582 0l7 6A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z'),
  ],
  check: [path('M20 6 9 17l-5-5')],
  'grip-vertical': [
    { kind: 'circle', cx: 9, cy: 5, r: 1 },
    { kind: 'circle', cx: 9, cy: 12, r: 1 },
    { kind: 'circle', cx: 9, cy: 19, r: 1 },
    { kind: 'circle', cx: 15, cy: 5, r: 1 },
    { kind: 'circle', cx: 15, cy: 12, r: 1 },
    { kind: 'circle', cx: 15, cy: 19, r: 1 },
  ],
}

function renderShape(shape: IconShape, index: number) {
  switch (shape.kind) {
    case 'path':
      return <path key={index} d={shape.d} />
    case 'circle':
      return <circle key={index} cx={shape.cx} cy={shape.cy} r={shape.r} />
    case 'ellipse':
      return <ellipse key={index} cx={shape.cx} cy={shape.cy} rx={shape.rx} ry={shape.ry} />
    case 'line':
      return <line key={index} x1={shape.x1} y1={shape.y1} x2={shape.x2} y2={shape.y2} />
    case 'rect':
      return <rect key={index} x={shape.x} y={shape.y} width={shape.width} height={shape.height} rx={shape.rx} />
  }
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
      {shapes[name].map(renderShape)}
    </svg>
  )
}
