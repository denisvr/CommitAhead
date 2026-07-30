import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DetailsSummary } from './DetailsSummary'

describe('DetailsSummary', () => {
  it('renders LeetCode details read-only', () => {
    render(
      <DetailsSummary
        details={{
          kind: 'LeetCode',
          problemNumber: 1,
          url: null,
          difficulty: 'Easy',
          patterns: ['two-pointers'],
          expectedTimeComplexity: 'O(n)',
          expectedSpaceComplexity: 'O(n)',
          approachMarkdown: 'Use a hash map.',
          cSharpSolution: null,
        }}
      />,
    )

    expect(screen.getByText('Easy')).toBeInTheDocument()
    expect(screen.getByText('two-pointers')).toBeInTheDocument()
    expect(screen.getByText('Use a hash map.')).toBeInTheDocument()
  })

  it('hides the SystemDesign reference solution until revealed', async () => {
    render(
      <DetailsSummary
        details={{
          kind: 'SystemDesign',
          promptMarkdown: 'Design a rate limiter.',
          clarifyingQuestions: [],
          functionalRequirements: [],
          nonFunctionalRequirements: [],
          evaluationChecklist: [],
          referenceSolutionMarkdown: 'Use a token bucket.',
        }}
      />,
    )

    expect(screen.queryByText('Use a token bucket.')).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Reveal reference solution' }))

    expect(screen.getByText('Use a token bucket.')).toBeInTheDocument()
  })
})
