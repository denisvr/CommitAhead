export interface DialogProps {
  open: boolean;
  title: string;
  children?: React.ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Renders the confirm action as the outlined danger variant. */
  destructive?: boolean;
  onConfirm?: () => void;
  onCancel?: () => void;
}
export declare function Dialog(props: DialogProps): JSX.Element | null;
