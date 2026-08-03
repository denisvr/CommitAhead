import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Brand, BookmarkMark } from './Brand'

describe('Brand', () => {
  it('renders the CommitAhead wordmark and mark', () => {
    render(<Brand />)

    expect(screen.getByText('Commit')).toBeInTheDocument()
    expect(screen.getByText('Ahead')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'CommitAhead' })).toBeInTheDocument()
  })
})

describe('BookmarkMark', () => {
  it('renders as a labelled image', () => {
    render(<BookmarkMark />)

    expect(screen.getByRole('img', { name: 'CommitAhead' })).toBeInTheDocument()
  })
})
