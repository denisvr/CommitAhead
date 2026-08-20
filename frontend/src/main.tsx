import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './design-system/styles.css'
import './index.css'
import { applyThemePreference, readThemePreference } from './design-system/theme'
import App from './App.tsx'

// Before the first paint, so an explicit light/dark choice does not flash the system theme first.
applyThemePreference(readThemePreference())

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
