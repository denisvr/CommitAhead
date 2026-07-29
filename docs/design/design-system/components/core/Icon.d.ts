export interface IconProps {
  /** Lucide glyph name, e.g. "check". Must be one of the 25 bundled in assets/icons/. */
  name: string;
  /** Pixel size. 16 in navigation and buttons, 20 maximum. */
  size?: number;
  /** Override the 1.75 sprite stroke. Rarely correct. */
  strokeWidth?: number;
  style?: React.CSSProperties;
}
export declare function Icon(props: IconProps): JSX.Element;
