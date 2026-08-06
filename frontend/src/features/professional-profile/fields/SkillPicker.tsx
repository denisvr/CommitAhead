import type { SkillDto } from '../api'
import styles from './SkillPicker.module.css'

type SkillPickerProps = {
  label: string
  skills: SkillDto[]
  value: string[]
  onChange: (skillIds: string[]) => void
}

// Experience/Project entries reference Skills by id (invariants 21/22 — ProfessionalProfile
// itself guards the reference and blocks deleting a referenced Skill); this is the one UI that
// needs that cross-collection reference, so it stays feature-local rather than a new
// design-system primitive.
export function SkillPicker({ label, skills, value, onChange }: SkillPickerProps) {
  const toggle = (skillId: string, checked: boolean) => {
    onChange(checked ? [...value, skillId] : value.filter((id) => id !== skillId))
  }

  if (skills.length === 0) {
    return <p className={styles.empty}>Add a skill first to reference it here.</p>
  }

  return (
    <fieldset className={styles.fieldset}>
      <legend className={styles.legend}>{label}</legend>
      <div className={styles.options}>
        {skills.map((skill) => (
          <label key={skill.id} className={styles.option}>
            <input type="checkbox" checked={value.includes(skill.id)} onChange={(event) => toggle(skill.id, event.target.checked)} />
            {skill.displayName}
          </label>
        ))}
      </div>
    </fieldset>
  )
}
