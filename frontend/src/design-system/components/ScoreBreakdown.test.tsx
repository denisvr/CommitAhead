import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ScoreBreakdown } from './ScoreBreakdown'

describe('ScoreBreakdown', () => {
  it('renders exactly the API-provided values, without recomputing anything', () => {
    // Deliberately inconsistent numbers (they don't sum to the score) — this component must
    // render whatever the API sent, never re-derive the total from the parts.
    render(<ScoreBreakdown effectiveScore={72} importanceContribution={10} demandContribution={5} masteryGapContribution={1} />)

    expect(screen.getByText('72')).toBeInTheDocument()
    expect(screen.getByText('10.0')).toBeInTheDocument()
    expect(screen.getByText('5.0')).toBeInTheDocument()
    expect(screen.getByText('1.0')).toBeInTheDocument()
  })
})
