export interface ScoreBreakdownProps {
  /** Points contributed by (importance/5) × importanceWeight. Default weights are 40/35/25. */
  importance: number;
  demand: number;
  masteryGap: number;
  /** "rows" for the labelled breakdown beside a numeral; "bar" for the 4px inline strip in a list row. */
  variant?: 'rows' | 'bar';
  width?: number;
  style?: React.CSSProperties;
}
export declare function ScoreBreakdown(props: ScoreBreakdownProps): JSX.Element;
