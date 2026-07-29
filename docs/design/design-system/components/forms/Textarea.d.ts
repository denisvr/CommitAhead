export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean;
  /** Set for Markdown and pasted job text, where character alignment matters. */
  mono?: boolean;
}
export declare function Textarea(props: TextareaProps): JSX.Element;
