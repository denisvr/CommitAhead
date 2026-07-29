export interface ProposalRowProps {
  /** "Link proposal" | "Study item proposal" | "Suggestion" — the AnalysisDraft proposal type. */
  kind: string;
  children?: React.ReactNode;
  /** The AI's stated reason. Always shown; a proposal without one is not reviewable. */
  rationale?: React.ReactNode;
  status?: 'pending' | 'accepted' | 'rejected';
  onAccept?: () => void;
  onReject?: () => void;
  style?: React.CSSProperties;
}
export declare function ProposalRow(props: ProposalRowProps): JSX.Element;
