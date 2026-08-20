// Theme preference, kept out of ThemeToggle.tsx so that file only exports a component (fast
// refresh) and so main.tsx can apply the stored choice before React mounts — otherwise the first
// paint uses the system theme and visibly flips.
//
// tokens/colors.css is authored for exactly three states: an explicit light choice, an explicit
// dark choice, and no choice at all, where the prefers-color-scheme block applies. 'system'
// therefore removes the attribute instead of resolving the preference here, leaving the browser as
// the single source of truth for it.
export type ThemePreference = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'commitahead.theme'

function isPreference(value: string | null): value is ThemePreference {
  return value === 'light' || value === 'dark' || value === 'system'
}

export function readThemePreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return isPreference(stored) ? stored : 'system'
  } catch {
    // Storage can throw when cookies/site data are blocked. A theme preference is not worth
    // failing a render over — fall back to following the system.
    return 'system'
  }
}

export function storeThemePreference(preference: ThemePreference) {
  try {
    localStorage.setItem(STORAGE_KEY, preference)
  } catch {
    // Same as above: the preference simply will not persist across reloads.
  }
}

export function applyThemePreference(preference: ThemePreference) {
  if (preference === 'system') document.documentElement.removeAttribute('data-theme')
  else document.documentElement.setAttribute('data-theme', preference)
}
