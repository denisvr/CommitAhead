/* Verifies every colour pair the Studio identity actually relies on, in both themes,
   by reading tokens/colors.css — so the numbers quoted in readme.md can never drift
   from the tokens themselves.

       node docs/design/design-system/verify-contrast.mjs

   Exits non-zero on any failure. This is a design-reference check, not part of the
   application build; run it whenever a colour token changes. */

import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))
// Newlines are normalised so the block matching below cannot break on a CRLF checkout.
const css = readFileSync(join(here, 'tokens', 'colors.css'), 'utf8').replace(/\r\n/g, '\n')

/* ---------- read the two themes out of the stylesheet ---------- */

function block(startPattern) {
  const start = css.indexOf(startPattern)
  if (start < 0) throw new Error(`could not find block: ${startPattern}`)
  const open = css.indexOf('{', start)
  let depth = 0
  for (let i = open; i < css.length; i++) {
    if (css[i] === '{') depth++
    else if (css[i] === '}') { depth--; if (depth === 0) return css.slice(open + 1, i) }
  }
  throw new Error(`unterminated block: ${startPattern}`)
}

function tokens(text) {
  const out = {}
  for (const [, name, value] of text.matchAll(/(--[\w-]+)\s*:\s*([^;]+);/g)) out[name] = value.trim()
  return out
}

const LIGHT = tokens(block(':root,\n:root[data-theme="light"]'))
const DARK = { ...LIGHT, ...tokens(block(':root[data-theme="dark"]')) }
const SYSTEM_DARK = tokens(block('@media (prefers-color-scheme: dark)'))

/* Resolve var() indirection so semantic aliases can be checked directly. */
function resolve(theme, name, seen = new Set()) {
  let value = theme[name]
  if (value === undefined) throw new Error(`undefined token ${name}`)
  const ref = value.match(/^var\((--[\w-]+)\)$/)
  if (!ref) return value
  if (seen.has(name)) throw new Error(`circular token ${name}`)
  seen.add(name)
  return resolve(theme, ref[1], seen)
}

/* ---------- WCAG relative luminance ---------- */

function rgb(value) {
  const hex = value.match(/^#([0-9a-f]{6})$/i)
  if (hex) { const n = parseInt(hex[1], 16); return [n >> 16 & 255, n >> 8 & 255, n & 255] }
  throw new Error(`not a plain hex colour: ${value}`)
}
const channel = c => { c /= 255; return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4 }
const luminance = value => { const [r, g, b] = rgb(value).map(channel); return 0.2126 * r + 0.7152 * g + 0.0722 * b }
function contrast(a, b) {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x)
  return (hi + 0.05) / (lo + 0.05)
}

/* ---------- the pairs the product actually renders ----------
   4.5  body text (WCAG 1.4.3 AA at our sizes)
   3.0  control edges and focus rings (WCAG 1.4.11 non-text contrast)
   <2   structural separation — our own legibility floor, not a WCAG rule */

const PAIRS = [
  ['--text', '--surface', 4.5, 'body text on a card'],
  ['--text', '--bg', 4.5, 'body text on the page'],
  ['--text-muted', '--surface', 4.5, 'secondary text on a card'],
  ['--text-muted', '--surface-sunken', 4.5, 'secondary text on a sunken region'],
  ['--text-muted', '--bg', 4.5, 'secondary text on the page'],
  ['--text-faint', '--surface', 4.5, 'metadata on a card'],
  ['--text-faint', '--surface-sunken', 4.5, 'metadata on a sunken region'],
  ['--text-faint', '--bg', 4.5, 'metadata on the page'],

  ['--accent', '--surface', 4.5, 'link/accent text on a card'],
  ['--accent', '--bg', 4.5, 'link/accent text on the page'],
  ['--accent', '--accent-wash', 4.5, 'accent text on its own wash'],
  ['--accent-contrast', '--accent', 4.5, 'label on a primary button'],

  ['--good', '--good-wash', 4.5, 'success badge'],
  ['--good', '--surface', 4.5, 'success text on a card'],
  ['--caution', '--caution-wash', 4.5, 'recommendation badge'],
  ['--caution', '--surface', 4.5, 'recommendation text on a card'],
  ['--critical', '--critical-wash', 4.5, 'error badge'],
  ['--critical', '--surface', 4.5, 'error text on a card'],

  ['--border-strong', '--surface', 3, 'input border on a card'],
  ['--border-strong', '--surface-sunken', 3, 'input border on a sunken region'],
  ['--focus-ring', '--surface', 3, 'focus ring on a card'],
  ['--focus-ring', '--bg', 3, 'focus ring on the page'],

  ['--surface', '--bg', 1.10, 'card lift away from the page'],
  ['--card-border', '--surface', 1.10, 'card edge'],
  ['--border-soft', '--surface', 1.08, 'hairline between rows'],
  ['--surface-alt', '--surface', 1.06, 'hover fill / meter track'],
]

function run(name, theme) {
  console.log(`\n──── ${name}`)
  let failed = 0
  for (const [fg, bg, min, label] of PAIRS) {
    const value = contrast(resolve(theme, fg), resolve(theme, bg))
    const ok = value + 1e-9 >= min
    if (!ok) failed++
    console.log(`  ${ok ? 'ok  ' : 'FAIL'} ${value.toFixed(2).padStart(6)}  (min ${min})  ${label}`)
  }
  return failed
}

let failures = run('light', LIGHT) + run('dark', DARK)

/* The explicit dark block and the system-preference block must stay identical.
   Nothing else catches drift between them. */
console.log('\n──── dark authored twice: explicit choice vs system preference')
const explicitDark = tokens(block(':root[data-theme="dark"]'))
const drift = [...new Set([...Object.keys(explicitDark), ...Object.keys(SYSTEM_DARK)])]
  .filter(key => explicitDark[key] !== SYSTEM_DARK[key])
for (const key of drift) {
  console.log(`  FAIL ${key}: explicit=${explicitDark[key] ?? 'missing'} system=${SYSTEM_DARK[key] ?? 'missing'}`)
}
if (!drift.length) console.log(`  ok    ${Object.keys(explicitDark).length} tokens identical`)
failures += drift.length

console.log(failures ? `\n${failures} failure(s)` : '\nall pairs pass')
process.exit(failures ? 1 : 0)
