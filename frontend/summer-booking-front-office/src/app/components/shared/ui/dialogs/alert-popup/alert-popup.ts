import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Button } from '../../buttons/button/button';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

/** Texts of `app-alert-popup`, already translated; a missing one is left out. */
export interface AlertPopupTexts {
  /** Text of the button that closes the popup (e.g. "Ho capito"). */
  close?: string;
}

/**
 * Generic alert popup (Angular Material dialog): warning icon, title, text and a button that closes it.
 * `<app-alert-popup [title]="…" [text]="…" [texts]="…" [(open)]="failed" />`, all already translated.
 * Screen readers announce it as an alert; Esc, a click outside or the button close it.
 * Icons (see `Icons`): [icon above the title]; without them, no icon. Texts: `AlertPopupTexts`.
 */
@Component({
  selector: 'app-alert-popup',
  imports: [Button, MatIconModule],
  templateUrl: './alert-popup.html',
  styleUrl: './alert-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class AlertPopup {
  /** Shows the popup when true; goes back to false when it is closed. */
  readonly open = model(false);
  /** Title, already translated. */
  readonly title = input<string>();
  /** Text, already translated. */
  readonly text = input<string>();
  readonly texts = input<AlertPopupTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [icon above the title]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon above the title]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly id = `alert-popup-${nextId++}`;

  private readonly dialog = inject(MatDialog);
  private readonly content = viewChild.required<TemplateRef<unknown>>('content');
  private dialogRef?: MatDialogRef<unknown>;

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.show() : this.close()));
    });
    inject(DestroyRef).onDestroy(() => this.close());
  }

  protected close(): void {
    this.dialogRef?.close();
  }

  private show(): void {
    if (this.dialogRef) {
      return;
    }
    const dialogRef = this.dialog.open(this.content(), {
      role: 'alertdialog',
      ariaLabelledBy: `${this.id}-title`,
      ariaDescribedBy: `${this.id}-text`,
      width: 'min(28rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'alert__popup__panel',
    });
    this.dialogRef = dialogRef;
    dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = undefined;
      this.open.set(false);
    });
  }
}
