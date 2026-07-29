export interface ChipProps {
  children?: React.ReactNode;
  /** Filled navy when the filter is active. */
  selected?: boolean;
  as?: 'span' | 'div';
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function Chip(props: ChipProps): JSX.Element;
