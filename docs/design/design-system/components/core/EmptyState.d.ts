export interface EmptyStateProps {
  title: string;
  children?: React.ReactNode;
  /** Usually a single <Button>. */
  action?: React.ReactNode;
  style?: React.CSSProperties;
}
export declare function EmptyState(props: EmptyStateProps): JSX.Element;
