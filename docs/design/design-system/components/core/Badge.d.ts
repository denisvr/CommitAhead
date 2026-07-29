export interface BadgeProps {
  children?: React.ReactNode;
  /** critical/caution/good = JobGap severity. draft = AI output awaiting confirmation. */
  tone?: 'critical' | 'caution' | 'good' | 'draft' | 'neutral';
  dot?: boolean;
  style?: React.CSSProperties;
}
export declare function Badge(props: BadgeProps): JSX.Element;
