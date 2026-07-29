export interface ButtonProps {
  children?: React.ReactNode;
  /** primary = the one action on the screen. danger is outlined and always needs a second confirmation. */
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  size?: 'md' | 'sm';
  /** Lucide glyph name shown before the label. */
  icon?: string;
  /** Lucide glyph name shown after the label. */
  iconEnd?: string;
  disabled?: boolean;
  fullWidth?: boolean;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  style?: React.CSSProperties;
}
export declare function Button(props: ButtonProps): JSX.Element;
