export interface NavItem { id: string; label: string; icon: string }
export interface SidebarNavProps {
  /** Defaults to the product's fixed six destinations. */
  items?: NavItem[];
  active?: string;
  onNavigate?: (id: string) => void;
  /** Rendered at the bottom — theme toggle, AI budget line. */
  footer?: React.ReactNode;
  style?: React.CSSProperties;
}
export declare const NAV_ITEMS: NavItem[];
export declare function SidebarNav(props: SidebarNavProps): JSX.Element;
