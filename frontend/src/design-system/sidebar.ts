// Sidebar collapse preference, kept out of Sidebar.tsx for the same fast-refresh reason as
// theme.ts (that file only exports a component).
const STORAGE_KEY = 'commitahead.sidebar-collapsed'

export function readSidebarCollapsed(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'true'
  } catch {
    // Storage can throw when cookies/site data are blocked. A collapse preference is not worth
    // failing a render over — fall back to expanded.
    return false
  }
}

export function storeSidebarCollapsed(collapsed: boolean) {
  try {
    localStorage.setItem(STORAGE_KEY, String(collapsed))
  } catch {
    // Same as above: the preference simply will not persist across reloads.
  }
}
