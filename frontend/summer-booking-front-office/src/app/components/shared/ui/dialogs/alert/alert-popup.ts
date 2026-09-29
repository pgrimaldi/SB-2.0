import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  ViewEncapsulation,
  effect,
  inject,
  input,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonComponent } from '../../buttons/button/button.component';

let nextId = 0;

/**
 * Generic alert popup (Angular Material dialog): warning icon, title, text and a button that closes it.
 * `<app-alert-popup title="…" text="…" [(open)]="failed" />`, with translation keys for title and text.
 * Screen readers announce it as an alert; Esc, a click outside or the button close it.
 */
@Component({
  selector: 'app-alert-popup',
  imports: [ButtonComponent, TranslatePipe],
  templateUrl: './alert-popup.html',
  styleUrl: './alert-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class AlertPopup {
  /** Shows the popup when true; goes back to false when it is closed. */
  readonly open = model(false);
  /** Translation key of the title. */
  readonly title = input.required<string>();
  /** Translation key of the text. */
  readonly text = input.required<string>();

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
