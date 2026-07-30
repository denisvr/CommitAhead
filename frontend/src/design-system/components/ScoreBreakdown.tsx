import styles from './ScoreBreakdown.module.css'

type ScoreBreakdownProps = {
  effectiveScore: number
  importanceContribution: number
  demandContribution: number
  masteryGapContribution: number
}

// Displays the API-provided EffectiveScore and its three weighted contributions
// (components.md "ScoreNumeral and ScoreBreakdown") — it explains ranking, it never calculates
// it. Purely textual, so the accessible description is the visible content itself.
export function ScoreBreakdown({ effectiveScore, importanceContribution, demandContribution, masteryGapContribution }: ScoreBreakdownProps) {
  return (
    <div className={styles.wrapper}>
      <p className={styles.numeral}>{effectiveScore}</p>
      <dl className={styles.breakdown}>
        <div className={styles.row}>
          <dt>Importance</dt>
          <dd>{importanceContribution.toFixed(1)}</dd>
        </div>
        <div className={styles.row}>
          <dt>Demand</dt>
          <dd>{demandContribution.toFixed(1)}</dd>
        </div>
        <div className={styles.row}>
          <dt>Mastery gap</dt>
          <dd>{masteryGapContribution.toFixed(1)}</dd>
        </div>
      </dl>
    </div>
  )
}
