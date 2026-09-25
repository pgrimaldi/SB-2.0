import { DOCUMENT } from '@angular/common';
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
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';
import { ButtonComponent } from '../../shared/ui/buttons/button/button.component';
import { Checkbox } from '../../shared/ui/checkboxes/checkbox/checkbox';
import { PasswordField } from '../../shared/ui/inputs/password-field/password-field';
import { TextField } from '../../shared/ui/inputs/text-field/text-field';

/** Popup size as a percentage of the screen. Without a height the popup fits its content. */
export interface LoginDialogSize {
  width: number;
  height?: number;
}

/** Sizes for regular screens and for phones (below the 48rem mobile breakpoint). */
export interface LoginDialogSizes {
  desktop: LoginDialogSize;
  mobile: LoginDialogSize;
}

const DEFAULT_SIZES: LoginDialogSizes = {
  desktop: { width: 30 },
  mobile: { width: 90 },
};

// Narrow windows (e.g. tablets) keep a usable form; content taller than the screen scrolls inside.
const MIN_WIDTH = 'min(26rem, 90vw)';
const MAX_HEIGHT = '90svh';
const MOBILE_QUERY = '(width < 48rem)';

/**
 * Hub login popup (wrapper around the Angular Material dialog): `<app-login-dialog [(open)]="loginOpen" />`.
 * Sign-in, password recovery and registration are not wired yet.
 */
@Component({
  selector: 'app-login-dialog',
  imports: [ButtonComponent, Checkbox, I18nText, PasswordField, TextField, TranslatePipe],
  templateUrl: './login-dialog.html',
  styleUrl: './login-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})

export class LoginDialog {
  /** Shows the popup when true; goes back to false when the popup is closed (X, Esc or click outside). */
  readonly open = model(false);
  readonly sizes = input<LoginDialogSizes>(DEFAULT_SIZES);

  private readonly dialog = inject(MatDialog);
  private readonly window = inject(DOCUMENT).defaultView;
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

    const mobile = this.window?.matchMedia?.(MOBILE_QUERY);
    const size = () => (mobile?.matches ? this.sizes().mobile : this.sizes().desktop);
    const width = () => `${size().width}vw`;
    const height = () => (size().height ? `${size().height}vh` : '');

    const dialogRef = this.dialog.open(this.content(), {
      width: width(),
      height: height(),
      minWidth: MIN_WIDTH,
      maxWidth: 'none',
      maxHeight: MAX_HEIGHT,
      panelClass: 'login__dialog__panel',
      ariaLabelledBy: 'login-dialog-title',
    });
    this.dialogRef = dialogRef;

    // Keep the right size if the phone is rotated or the window resized while the popup is open.
    const resize = () => dialogRef.updateSize(width(), height());
    mobile?.addEventListener('change', resize);

    dialogRef.afterClosed().subscribe(() => {
      mobile?.removeEventListener('change', resize);
      this.dialogRef = undefined;
      this.open.set(false);
    });
  }
}
