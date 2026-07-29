export interface FieldProps {
  label?: string;
  /** Shown under the control. Replaced by `error` when present. */
  hint?: string;
  error?: string;
  htmlFor?: string;
  children?: React.ReactNode;
  style?: React.CSSProperties;
}
export declare function Field(props: FieldProps): JSX.Element;
