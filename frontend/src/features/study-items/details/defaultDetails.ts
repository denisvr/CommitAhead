import type { StudyItemCategory, StudyItemDetailsDto } from '../api'

export function defaultDetailsFor(category: StudyItemCategory): StudyItemDetailsDto {
  switch (category) {
    case 'LeetCode':
      return {
        kind: 'LeetCode',
        problemNumber: null,
        url: null,
        difficulty: 'Easy',
        patterns: [],
        expectedTimeComplexity: '',
        expectedSpaceComplexity: '',
        approachMarkdown: '',
        cSharpSolution: null,
      }
    case 'SystemDesign':
      return {
        kind: 'SystemDesign',
        promptMarkdown: '',
        clarifyingQuestions: [],
        functionalRequirements: [],
        nonFunctionalRequirements: [],
        evaluationChecklist: [],
        referenceSolutionMarkdown: '',
      }
    case 'Behavioral':
      return { kind: 'Behavioral', competencies: [], questionVariants: [], situation: '', task: '', action: '', result: '', reflection: null }
    case 'Theory':
      return { kind: 'Theory', summaryMarkdown: '', keyPoints: [], interviewQuestions: [], references: [] }
  }
}
