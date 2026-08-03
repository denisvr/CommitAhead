import Markdown, { type Components } from 'react-markdown'
import { restrictedUrlTransform } from './restrictedUrlTransform'

const components: Components = {
  // No images anywhere in restricted Markdown — not even from an allowed-scheme URL.
  img: () => null,
  a({ href, children, ...rest }) {
    // restrictedUrlTransform already returned '' for a disallowed scheme, so an empty href here
    // means "don't render a link at all" — the text still shows, just not as a clickable anchor.
    if (!href) {
      return <>{children}</>
    }

    return (
      <a href={href} target="_blank" rel="noopener noreferrer nofollow" {...rest}>
        {children}
      </a>
    )
  },
}

type RestrictedMarkdownProps = {
  children: string
  className?: string
}

/**
 * The one reusable Markdown renderer for every Phase 1 Markdown field (review notes, typed-detail
 * narrative fields) — and, later, AI-generated content. react-markdown never parses raw HTML into
 * real elements unless the rehype-raw plugin is added (it isn't, here): a <script>/<iframe>/any
 * tag in the source renders as literal escaped text, never executes and never embeds anything. No
 * dangerouslySetInnerHTML anywhere in this component.
 */
export function RestrictedMarkdown({ children, className }: RestrictedMarkdownProps) {
  return (
    <div className={className}>
      <Markdown urlTransform={restrictedUrlTransform} components={components}>
        {children}
      </Markdown>
    </div>
  )
}
