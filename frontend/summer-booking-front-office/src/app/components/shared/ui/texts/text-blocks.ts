import { TextPart, boldParts } from './bold-text';

export enum TextBlockKind {
  Paragraph,
  Heading,
  List,
}

export interface TextListItem {
  readonly parts: readonly TextPart[];
  /** The points under it. */
  readonly children: readonly (readonly TextPart[])[];
}

export interface TextBlock {
  readonly kind: TextBlockKind;
  /** The text of a paragraph or a heading. */
  readonly parts: readonly TextPart[];
  /** The points of a list. */
  readonly items: readonly TextListItem[];
  /** A blank line before it. */
  readonly hasSpaceBefore: boolean;
}

interface ListItemDraft {
  parts: TextPart[];
  children: TextPart[][];
}

/**
 * Splits an already translated text into blocks, without HTML: a line starting with `## ` is a
 * heading, `- ` a point of a list, `  - ` a point under the one above; the other lines are paragraphs
 * that keep their line breaks, and a blank line leaves a space. In every block `**…**` is bold.
 */
export function textBlocks(text: string | null | undefined): TextBlock[] {
  const blocks: TextBlock[] = [];
  let paragraph: string[] = [];
  let items: ListItemDraft[] = [];
  let hasSpaceBefore = false;

  const push = (block: Omit<TextBlock, 'hasSpaceBefore'>): void => {
    blocks.push({ ...block, hasSpaceBefore });
    hasSpaceBefore = false;
  };
  const closeParagraph = (): void => {
    if (paragraph.length) {
      push({ kind: TextBlockKind.Paragraph, parts: boldParts(paragraph.join('\n')), items: [] });
      paragraph = [];
    }
  };
  const closeList = (): void => {
    if (items.length) {
      push({ kind: TextBlockKind.List, parts: [], items });
      items = [];
    }
  };

  for (const line of (text ?? '').split('\n')) {
    const heading = /^## (.*)$/.exec(line);
    const item = /^- (.*)$/.exec(line);
    const child = /^\s{2,}- (.*)$/.exec(line);
    if (heading) {
      closeParagraph();
      closeList();
      push({ kind: TextBlockKind.Heading, parts: boldParts(heading[1]), items: [] });
    } else if (item) {
      closeParagraph();
      items.push({ parts: boldParts(item[1]), children: [] });
    } else if (child) {
      closeParagraph();
      const parent = items.at(-1);
      if (parent) {
        parent.children.push(boldParts(child[1]));
      } else {
        items.push({ parts: boldParts(child[1]), children: [] });
      }
    } else if (!line.trim()) {
      closeParagraph();
      closeList();
      hasSpaceBefore = blocks.length > 0;
    } else {
      closeList();
      paragraph.push(line);
    }
  }
  closeParagraph();
  closeList();
  return blocks;
}
