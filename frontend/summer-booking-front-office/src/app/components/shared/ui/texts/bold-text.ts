export interface TextPart {
  readonly text: string;
  readonly isBold: boolean;
}

/**
 * Splits an already translated text at its `**` marks, without HTML: `'Se **è abilitata**, …'`
 * gives the plain `'Se '`, the bold `'è abilitata'` and the plain `', …'`.
 */
export function boldParts(text: string | null | undefined): TextPart[] {
  return (text ?? '')
    .split('**')
    .map((part, index) => ({ text: part, isBold: index % 2 === 1 }))
    .filter((part) => part.text);
}
