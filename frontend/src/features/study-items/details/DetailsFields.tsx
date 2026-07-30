import type { StudyItemCategory, StudyItemDetailsDto } from '../api'
import { LeetCodeFields } from './LeetCodeFields'
import { SystemDesignFields } from './SystemDesignFields'
import { BehavioralFields } from './BehavioralFields'
import { TheoryFields } from './TheoryFields'
import type { BehavioralDetailsValue, LeetCodeDetailsValue, SystemDesignDetailsValue, TheoryDetailsValue } from './types'

type DetailsFieldsProps = {
  category: StudyItemCategory
  value: StudyItemDetailsDto
  onChange: (value: StudyItemDetailsDto) => void
}

// Dispatches to the typed field set matching the StudyItem's fixed category (ADR-0001) — the
// discriminated union is a compile-time guarantee here, not something this component re-validates.
export function DetailsFields({ category, value, onChange }: DetailsFieldsProps) {
  switch (category) {
    case 'LeetCode':
      return <LeetCodeFields value={value as LeetCodeDetailsValue} onChange={onChange} />
    case 'SystemDesign':
      return <SystemDesignFields value={value as SystemDesignDetailsValue} onChange={onChange} />
    case 'Behavioral':
      return <BehavioralFields value={value as BehavioralDetailsValue} onChange={onChange} />
    case 'Theory':
      return <TheoryFields value={value as TheoryDetailsValue} onChange={onChange} />
  }
}
