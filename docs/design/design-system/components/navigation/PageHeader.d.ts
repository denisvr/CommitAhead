export interface PageHeaderProps {
  /** Mono uppercase line above the title — date, count, context. */
  kicker?: string;
  title: string;
  /** One sentence stating what is shown and how it is sorted. Required on every list screen. */
  summary?: string;
  actions?: React.ReactNode;
  style?: React.CSSProperties;
}
export declare function PageHeader(props: PageHeaderProps): JSX.Element;
