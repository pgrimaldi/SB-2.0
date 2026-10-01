import {
  Directive,
  OnDestroy,
  TemplateRef,
  effect,
  inject,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { MatDialog, MatDialogConfig, MatDialogRef } from '@angular/material/dialog';

/**
 * Base of the app's popups (Angular Material dialog), used declaratively: `[(open)]="…"`.
 * It opens and closes the dialog, and closes it when the page goes away. A popup extends it, puts its
 * content in an `<ng-template #content>` of its template and gives the dialog options (`dialogConfig`).
 * A closed popup is closed at once, without waiting for the closing animation: a new `open` right
 * after (e.g. an error again after "retry") opens it again.
 */
@Directive()
export abstract class BasePopup implements OnDestroy {
  /** Shows the popup when true; goes back to false as soon as it is closed. */
  readonly open = model(false);

  private readonly dialog = inject(MatDialog);

  private readonly content = viewChild.required<TemplateRef<unknown>>('content');
  private dialogRef?: MatDialogRef<unknown>;

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.show() : this.close()));
    });
  }

  /** Options of the Material dialog: role, accessible name and description, size, panel class. */
  protected abstract dialogConfig(): MatDialogConfig;

  /** Closes at once, without waiting for the closing animation. */
  protected close(): void {
    const dialogRef = this.dialogRef;
    this.dialogRef = undefined;
    this.open.set(false);
    dialogRef?.close();
  }

  private show(): void {
    if (this.dialogRef) {
      return;
    }
    const dialogRef = this.dialog.open(this.content(), this.dialogConfig());
    this.dialogRef = dialogRef;
    dialogRef.afterClosed().subscribe(() => this.closed(dialogRef));
  }

  /** The popup finished closing: by Esc or a click outside it is still the current one. */
  private closed(dialogRef: MatDialogRef<unknown>): void {
    if (this.dialogRef === dialogRef) {
      this.close();
    }
  }

  /** Leaving the page closes the popup, without telling a page that no longer exists. */
  ngOnDestroy(): void {
    const dialogRef = this.dialogRef;
    this.dialogRef = undefined;
    dialogRef?.close();
  }
}
