export interface RatingScaleProps {
  /** 1–5. Importance, InitialMastery, and StudyReview confidence all use this range. */
  value?: number;
  onChange?: (value: number) => void;
  /** Accessible group name, e.g. "Confidence rating". */
  name?: string;
  min?: number;
  max?: number;
  disabled?: boolean;
  style?: React.CSSProperties;
}
export declare function RatingScale(props: RatingScaleProps): JSX.Element;
