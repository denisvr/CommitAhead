export interface DataColumn {
  key: string;
  label: string;
  /** Any CSS grid track, e.g. "1fr" or "96px". */
  width?: string;
  align?: 'left' | 'right';
  /** Mono + tabular figures. Set on every numeric column. */
  mono?: boolean;
  muted?: boolean;
}
export interface DataTableProps {
  columns: DataColumn[];
  rows: Array<Record<string, React.ReactNode> & { id?: string }>;
  onRowClick?: (row: any) => void;
  style?: React.CSSProperties;
}
export declare function DataTable(props: DataTableProps): JSX.Element;
