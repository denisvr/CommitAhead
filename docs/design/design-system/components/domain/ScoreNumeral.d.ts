export interface ScoreNumeralProps {
  /** Integer 0–100. Never rounded for display, never shown as a percentage. */
  value: number;
  label?: string;
  size?: number;
  align?: 'left' | 'right';
  style?: React.CSSProperties;
}
export declare function ScoreNumeral(props: ScoreNumeralProps): JSX.Element;
