import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  effect,
  input,
  model,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import Bold from '@tiptap/extension-bold';
import Document from '@tiptap/extension-document';
import HardBreak from '@tiptap/extension-hard-break';
import Italic from '@tiptap/extension-italic';
import Link from '@tiptap/extension-link';
import { BulletList, ListItem, OrderedList } from '@tiptap/extension-list';
import Paragraph from '@tiptap/extension-paragraph';
import Text from '@tiptap/extension-text';
import Underline from '@tiptap/extension-underline';
import { UndoRedo } from '@tiptap/extensions';
import { Button, ButtonSize } from '../../buttons/button/button';
import { IconButton } from '../../buttons/icon-button/icon-button';
import { resolveIcons } from '../../icons/icons';
import { FilledTextField } from '../filled-text-field/filled-text-field';

export interface RichTextFieldTexts {
  bold?: string;
  italic?: string;
  underline?: string;
  bullet_list?: string;
  ordered_list?: string;
  link?: string;
  link_placeholder?: string;
  insert_link?: string;
}

enum RichTextAction {
  Bold,
  Italic,
  Underline,
  BulletList,
  OrderedList,
  Link,
}

/** In the order of the icons. */
const TOOLBAR = [
  { action: RichTextAction.Bold, mark: 'bold', text: 'bold' },
  { action: RichTextAction.Italic, mark: 'italic', text: 'italic' },
  { action: RichTextAction.Underline, mark: 'underline', text: 'underline' },
  { action: RichTextAction.BulletList, mark: 'bulletList', text: 'bullet_list' },
  { action: RichTextAction.OrderedList, mark: 'orderedList', text: 'ordered_list' },
  { action: RichTextAction.Link, mark: 'link', text: 'link' },
] as const;

/** A scheme (https:, mailto:…) at the start of the address. */
const SCHEME = /^[a-z][a-z\d+.-]*:/i;

let nextId = 0;

/**
 * Text with bold, italic, underline, bulleted and numbered lists and links, on TipTap (ProseMirror).
 * The value is HTML made only of those elements (the editor drops anything else, pasted content
 * too), or null when empty. Links: only safe schemes (no `javascript:`), opened in a new tab.
 */
@Component({
  selector: 'app-rich-text-field',
  imports: [Button, FilledTextField, IconButton],
  templateUrl: './rich-text-field.html',
  styleUrl: './rich-text-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RichTextField implements OnDestroy {
  readonly label = input<string>();
  /** Asterisk after the label. */
  readonly isMandatory = input(false);
  /** Shown but not editable; a Signal Forms `[formField]` sets it from the `readonly` rule. */
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<string | null>(null);
  readonly error = input<string | null>();
  readonly texts = input<RichTextFieldTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [bold, italic, underline, bulleted list, numbered list, link]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Image paths, used when `matIcon` is not given: same order. */
  readonly pathIcon = input<readonly string[] | null>();
  /** The field was left: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  protected readonly ButtonSize = ButtonSize;
  protected readonly id = `rich-text-field-${nextId++}`;
  protected readonly isLocked = computed(() => this.readonly() || this.disabled());
  protected readonly buttons = computed(() => {
    const icons = resolveIcons(this.matIcon(), this.pathIcon());
    return TOOLBAR.map(({ action, mark, text }, index) => {
      const icon = icons.list.slice(index, index + 1);
      return {
        action,
        mark,
        label: this.texts()?.[text] ?? '',
        matIcon: icons.material ? icon : null,
        pathIcon: icons.material ? null : icon,
      };
    });
  });
  protected readonly isLinkOpen = signal(false);
  protected readonly linkUrl = signal<string | null>(null);
  /** Changes with every change of the editor (text or cursor): the buttons read their state again. */
  private readonly revision = signal(0);
  private readonly content = viewChild.required<ElementRef<HTMLElement>>('content');
  private editor: Editor | null = null;
  /** The HTML the editor last gave to `value`: a value from outside different from it is loaded. */
  private shownValue: string | null = null;

  constructor() {
    afterNextRender(() => {
      this.shownValue = this.value();
      this.editor = new Editor({
        element: this.content().nativeElement,
        extensions: [
          Document,
          Paragraph,
          Text,
          HardBreak,
          Bold,
          Italic,
          Underline,
          BulletList,
          OrderedList,
          ListItem,
          UndoRedo,
          Link.configure({
            openOnClick: false,
            defaultProtocol: 'https',
            HTMLAttributes: { rel: 'noopener noreferrer nofollow', target: '_blank' },
          }),
        ],
        content: this.shownValue ?? '',
        editable: !this.isLocked(),
        // The styles the editor needs are in this component: no <style> added to the page.
        injectCSS: false,
        onUpdate: ({ editor }) => {
          this.shownValue = editor.isEmpty ? null : editor.getHTML();
          this.value.set(this.shownValue);
        },
        onTransaction: () => this.revision.update((revision) => revision + 1),
        onBlur: () => this.touch.emit(),
      });
    });
    effect(() => {
      const value = this.value();
      untracked(() => {
        if (this.editor && value !== this.shownValue) {
          this.shownValue = value;
          this.editor.commands.setContent(value ?? '', { emitUpdate: false });
        }
      });
    });
    effect(() => {
      const isEditable = !this.isLocked();
      untracked(() => this.editor?.setEditable(isEditable, false));
    });
  }

  protected isActive(mark: string): boolean {
    this.revision();
    return this.editor?.isActive(mark) ?? false;
  }

  protected run(action: RichTextAction): void {
    const chain = this.editor?.chain().focus();
    switch (action) {
      case RichTextAction.Bold:
        chain?.toggleBold().run();
        break;
      case RichTextAction.Italic:
        chain?.toggleItalic().run();
        break;
      case RichTextAction.Underline:
        chain?.toggleUnderline().run();
        break;
      case RichTextAction.BulletList:
        chain?.toggleBulletList().run();
        break;
      case RichTextAction.OrderedList:
        chain?.toggleOrderedList().run();
        break;
      case RichTextAction.Link:
        if (this.editor?.isActive('link')) {
          chain?.extendMarkRange('link').unsetLink().run();
        } else {
          this.linkUrl.set(null);
          this.isLinkOpen.update((isOpen) => !isOpen);
        }
        break;
    }
  }

  /** On the chosen text; with nothing chosen, the address itself becomes the linked text. */
  protected insertLink(): void {
    const url = this.linkUrl()?.trim();
    this.isLinkOpen.set(false);
    if (!url || !this.editor) {
      return;
    }
    const href = SCHEME.test(url) ? url : `https://${url}`;
    const chain = this.editor.chain().focus();
    if (this.editor.state.selection.empty) {
      chain
        .insertContent({ type: 'text', text: url, marks: [{ type: 'link', attrs: { href } }] })
        .run();
    } else {
      chain.extendMarkRange('link').setLink({ href }).run();
    }
  }

  protected focus(): void {
    this.editor?.commands.focus();
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
  }
}
