export interface BrandProps {
  /** Type size in px; the symbol scales from it. 17 in the sidebar, 30+ on login. */
  size?: number;
  /** Set false for text-only contexts such as an exported CV footer. */
  symbol?: boolean;
  style?: React.CSSProperties;
}
export declare function Brand(props: BrandProps): JSX.Element;
