export interface IconButtonProps {
  icon: string;
  /** Required — becomes aria-label and the tooltip. */
  label: string;
  size?: 'md' | 'sm';
  tone?: 'default' | 'danger';
  disabled?: boolean;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  style?: React.CSSProperties;
}
export declare function IconButton(props: IconButtonProps): JSX.Element;
