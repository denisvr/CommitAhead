export interface CalloutProps {
  children?: React.ReactNode;
  title?: string;
  tone?: 'info' | 'critical' | 'caution';
  style?: React.CSSProperties;
}
export declare function Callout(props: CalloutProps): JSX.Element;
