import {
  Directive,
  OnDestroy,
  TemplateRef,
  computed,
  effect,
  inject,
  input,
  model,
  untracked,
  viewChild,
} from '@angular/core';
import { MatDialog, MatDialogConfig, MatDialogRef } from '@angular/material/dialog';
import { resolveIcons } from '../../icons/icons';
import { PopupFrame, PopupFrameData } from '../popup-frame/popup-frame';

/**
 * A popup extends it, puts its content in an `<ng-template #content>` of its template and gives the
 * dialog options (`dialogConfig`). The X in the top corner is drawn here for every popup: it is the
 * first icon of `matIcon` / `pathIcon`, and the popup's own icons follow it.
 */
@Directive()
export abstract class BasePopup implements OnDestroy {
  readonly open = model(false);
  /** Icons as Material icon names (Material Symbols font): [X, then the popup's own icons]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [X, then the popup's own icons]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly dialog = inject(MatDialog);

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  private readonly content = viewChild.required<TemplateRef<unknown>>('content');
  private dialogRef?: MatDialogRef<PopupFrame>;

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
   * Esc, a click outside or the X while they do not close the popup (`disableClose`,
   * `setClosable(false)`): the popup may close after asking (e.g. unsaved changes). Nothing by default.
   */
  protected closeRequested(): void {
    // Blocked: e.g. while saving.
  }

  /** The X cannot be pressed, e.g. while saving. */
  protected isCloseDisabled(): boolean {
    return false;
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
    const data: PopupFrameData = {
      content: this.content(),
      icons: () => this.icons(),
      isCloseDisabled: () => this.isCloseDisabled(),
      close: () => this.closeByX(),
    };
    const dialogRef = this.dialog.open(PopupFrame, { ...this.dialogConfig(), data });
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

  /** The X does what Esc does. */
  private closeByX(): void {
    const dialogRef = this.dialogRef;
    if (!dialogRef || this.isCloseDisabled()) {
      return;
    }
    if (dialogRef.disableClose) {
      this.closeRequested();
    } else {
      this.close();
    }
  }

  private blockedClose(dialogRef: MatDialogRef<PopupFrame>): void {
    if (dialogRef.disableClose && this.dialogRef === dialogRef) {
      this.closeRequested();
    }
  }

  /** The popup finished closing: by Esc or a click outside it is still the current one. */
  private closed(dialogRef: MatDialogRef<PopupFrame>): void {
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
