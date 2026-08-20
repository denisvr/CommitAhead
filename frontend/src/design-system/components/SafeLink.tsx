import type { ReactNode } from 'react'
import { restrictedUrl } from './restrictedUrlTransform'

// A raw user-entered URL field (not Markdown) rendered as a link only if its scheme survives
// restrictedUrl — the same allowlist RestrictedMarkdown applies to Markdown links, kept in sync
// deliberately since both render user-controlled URLs into a real href.
export function SafeLink({ url, children }: { url: string; children: ReactNode }) {
  const safeUrl = restrictedUrl(url)
  if (!safeUrl) {
    return <>{children}</>
  }

  return (
    <a href={safeUrl} target="_blank" rel="noopener noreferrer nofollow">
      {children}
    </a>
  )
}
