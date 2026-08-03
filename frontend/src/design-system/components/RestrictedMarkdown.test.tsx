import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { RestrictedMarkdown } from './RestrictedMarkdown'

describe('RestrictedMarkdown', () => {
  it('renders normal formatting (bold, list, heading)', () => {
    render(<RestrictedMarkdown>{'# Title\n\n**bold** and a list:\n\n- one\n- two'}</RestrictedMarkdown>)

    expect(screen.getByRole('heading', { name: 'Title' })).toBeInTheDocument()
    expect(screen.getByText('bold').tagName).toBe('STRONG')
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
  })

  it('never executes or embeds raw HTML tags', () => {
    const { container } = render(<RestrictedMarkdown>{'Before <strong class="injected">raw</strong> after'}</RestrictedMarkdown>)

    expect(container.querySelector('.injected')).toBeNull()
    expect(container.textContent).toContain('<strong class="injected">raw</strong>')
  })

  it('renders a script tag as literal text, never as a real element', () => {
    const { container } = render(<RestrictedMarkdown>{'<script>alert(1)</script>'}</RestrictedMarkdown>)

    expect(container.querySelector('script')).toBeNull()
    expect(container.textContent).toContain('<script>alert(1)</script>')
  })

  it('drops a javascript: link, rendering only the link text', () => {
    render(<RestrictedMarkdown>{'[click me](javascript:alert(1))'}</RestrictedMarkdown>)

    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.getByText('click me')).toBeInTheDocument()
  })

  it('drops a data: link, rendering only the link text', () => {
    render(<RestrictedMarkdown>{'[click me](data:text/html,<script>alert(1)</script>)'}</RestrictedMarkdown>)

    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.getByText('click me')).toBeInTheDocument()
  })

  it('never renders an image element', () => {
    const { container } = render(<RestrictedMarkdown>{'![alt text](https://example.com/pic.png)'}</RestrictedMarkdown>)

    expect(container.querySelector('img')).toBeNull()
  })

  it('renders safe https links with a real href and safe rel attributes', () => {
    render(<RestrictedMarkdown>{'[docs](https://example.com/docs)'}</RestrictedMarkdown>)

    const link = screen.getByRole('link', { name: 'docs' })
    expect(link).toHaveAttribute('href', 'https://example.com/docs')
    expect(link).toHaveAttribute('rel', expect.stringContaining('noopener'))
    expect(link).toHaveAttribute('target', '_blank')
  })

  it('renders safe http links', () => {
    render(<RestrictedMarkdown>{'[docs](http://example.com/docs)'}</RestrictedMarkdown>)

    expect(screen.getByRole('link', { name: 'docs' })).toHaveAttribute('href', 'http://example.com/docs')
  })

  it('renders safe mailto links', () => {
    render(<RestrictedMarkdown>{'[email me](mailto:owner@example.com)'}</RestrictedMarkdown>)

    expect(screen.getByRole('link', { name: 'email me' })).toHaveAttribute('href', 'mailto:owner@example.com')
  })
})
