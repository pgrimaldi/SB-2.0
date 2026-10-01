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
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { I18nText } from '../../i18n/i18n-text/i18n-text';
import { Button } from '../../shared/ui/buttons/button/button';
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
 * Password recovery and registration are not wired yet.
 */
@Component({
  selector: 'app-login-dialog',
  imports: [Button, Checkbox, I18nText, PasswordField, TextField, TranslatePipe],
  templateUrl: './login-dialog.html',
  styleUrl: './login-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class LoginDialog {
  /** Shows the popup when true; goes back to false when the popup is closed (X, Esc or click outside). */
  readonly open = model(false);
  readonly sizes = input<LoginDialogSizes>(DEFAULT_SIZES);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly remember = signal(false);
  protected readonly pending = signal(false);
  /** Error of the last sign-in, as the API answered it. */
  protected readonly error = signal<ApiProblem | null>(null);

  private readonly authBehaviour = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  /** Translation key of the error message (`error.<code>`, or `error.unknown`). */
  protected readonly errorKey = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.key(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly router = inject(Router);
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

  protected signIn(event: Event): void {
    event.preventDefault();
    if (this.pending()) {
      return;
    }

    this.pending.set(true);
    this.error.set(null);
    this.authBehaviour
      .signIn({
        username: this.email().trim(),
        password: this.password(),
        remember: this.remember(),
      })
      .subscribe({
        next: () => {
          this.close();
          void this.router.navigateByUrl('/beachmap');
        },
        error: (error: unknown) => {
          this.pending.set(false);
          // The message comes from the code of the error (e.g. wrong credentials, no connection).
          this.error.set(toApiProblem(error));
        },
      });
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
      this.reset();
      this.open.set(false);
    });
  }

  /** Every opening starts from an empty form. */
  private reset(): void {
    this.email.set('');
    this.password.set('');
    this.remember.set(false);
    this.pending.set(false);
    this.error.set(null);
  }
}
