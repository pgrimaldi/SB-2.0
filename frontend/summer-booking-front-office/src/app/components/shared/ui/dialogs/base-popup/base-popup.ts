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
 * A popup extends it, puts its content in an `<ng-template #content>` of its template and gives the
 * dialog options (`dialogConfig`).
 */
@Directive()
export abstract class BasePopup implements OnDestroy {
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

  protected abstract dialogConfig(): MatDialogConfig;

  /**
   * Closes at once, without waiting for the closing animation: a new `open` right after (e.g. an
   * error again after "retry") opens it again.
   */
  protected close(): void {
    const dialogRef = this.dialogRef;
    this.dialogRef = undefined;
    this.open.set(false);
    dialogRef?.close();
  }

  /**
   * Esc or a click outside while they do not close the popup (`disableClose`, `setClosable(false)`):
   * the popup may close after asking (e.g. unsaved changes). Nothing by default.
   */
  protected closeRequested(): void {
    // Blocked: e.g. while saving.
  }

  /** Blocks only Esc and a click outside (e.g. while saving): the popup blocks its own buttons. */
  protected setClosable(closable: boolean): void {
    if (this.dialogRef) {
      this.dialogRef.disableClose = !closable;
    }
  }

  private show(): void {
    if (this.dialogRef) {
      return;
    }
    const dialogRef = this.dialog.open(this.content(), this.dialogConfig());
    this.dialogRef = dialogRef;
    dialogRef.afterClosed().subscribe(() => this.closed(dialogRef));
    // Both end when the popup closes.
    dialogRef.backdropClick().subscribe(() => this.blockedClose(dialogRef));
    dialogRef.keydownEvents().subscribe((event) => {
      if (event.key === 'Escape') {
        this.blockedClose(dialogRef);
      }
    });
  }

  private blockedClose(dialogRef: MatDialogRef<unknown>): void {
    if (dialogRef.disableClose && this.dialogRef === dialogRef) {
      this.closeRequested();
    }
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
