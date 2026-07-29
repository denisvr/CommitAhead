export interface QueueRowProps {
  /** Zero-padded position, e.g. "02". It is the literal EffectiveScore ordering. */
  rank: string;
  title: string;
  /** One line of justification — why this item is here. */
  meta?: React.ReactNode;
  category: string;
  score: number;
  /** Renders the 4px segmented bar under the title (comfortable density only). */
  breakdown?: { importance: number; demand: number; masteryGap: number };
  dense?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function QueueRow(props: QueueRowProps): JSX.Element;
