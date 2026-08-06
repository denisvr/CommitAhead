import type { UrlTransform } from 'react-markdown'

// react-markdown's own default (safeProtocol in its source) also allows irc(s)/xmpp — narrower
// than what CommitAhead needs, so this replaces it rather than layering on top of it.
const ALLOWED_PROTOCOLS = /^(https?|mailto)$/i

/**
 * Mirrors react-markdown's own defaultUrlTransform's relative-URL detection (a colon that
 * appears after the first ?, #, or / is not a scheme separator, e.g. a query string containing
 * one) but narrows the scheme allowlist to exactly http(s)/mailto. Disallowed schemes —
 * javascript:, data:, anything else — return '', which callers turn into plain text instead of a
 * clickable link (RestrictedMarkdown's `a` override, or CVPreview's SafeLink for a raw URL field).
 */
export function restrictedUrl(url: string): string {
  const colon = url.indexOf(':')
  const questionMark = url.indexOf('?')
  const numberSign = url.indexOf('#')
  const slash = url.indexOf('/')

  const hasNoScheme = colon === -1 || (slash !== -1 && colon > slash) || (questionMark !== -1 && colon > questionMark) || (numberSign !== -1 && colon > numberSign)

  if (hasNoScheme) {
    return url
  }

  return ALLOWED_PROTOCOLS.test(url.slice(0, colon)) ? url : ''
}

export const restrictedUrlTransform: UrlTransform = (url) => restrictedUrl(url)
