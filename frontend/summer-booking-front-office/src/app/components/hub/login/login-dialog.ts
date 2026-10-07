import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
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
import { FormField, form, required, submit } from '@angular/forms/signals';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { SignInRequest } from '../../../entities/auth/credentials';
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

/** Password recovery and registration are not wired yet. */
@Component({
  selector: 'app-login-dialog',
  imports: [Button, Checkbox, FormField, I18nText, PasswordField, TextField, TranslatePipe],
  templateUrl: './login-dialog.html',
  styleUrl: './login-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class LoginDialog implements OnDestroy {
  /** Goes back to false when the popup is closed (X, Esc or click outside). */
  readonly open = model(false);
  readonly sizes = input<LoginDialogSizes>(DEFAULT_SIZES);

  private readonly authBehaviour = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly window = inject(DOCUMENT).defaultView;

  private readonly credentials = signal(new SignInRequest());
  protected readonly credentialsForm = form(
    this.credentials,
    (path) => {
      required(path.username);
      required(path.password);
    },
    { name: 'login' },
  );
  protected readonly usernameError = this.validationText.message(this.credentialsForm.username);
  protected readonly passwordError = this.validationText.message(this.credentialsForm.password);
  protected readonly pending = signal(false);
  protected readonly error = signal<ApiProblem | null>(null);
  protected readonly errorKey = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.key(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly content = viewChild.required<TemplateRef<unknown>>('content');
  private dialogRef?: MatDialogRef<unknown>;
  private mobileQuery?: MediaQueryList;
  /** Goes up at every opening: an attempt shows its error only in the opening it started from. */
  private opening = 0;

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.show() : this.close()));
    });
  }

  /** Closes at once, without waiting for the closing animation. */
  protected close(): void {
    const dialogRef = this.dialogRef;
    this.dialogRef = undefined;
    this.open.set(false);
    dialogRef?.close();
  }

  protected signIn(event: Event): void {
    event.preventDefault();
    if (this.pending()) {
      return;
    }
    // Every field becomes touched, so each shows its error; nothing is sent while one is wrong.
    void submit(this.credentialsForm, async () => {
      this.send();
      return undefined;
    });
  }

  private send(): void {
    // Closing the popup does not stop the attempt: the server may already have opened the session.
    // Until it answers the button keeps its spinner, also in the popup opened again.
    const opening = this.opening;
    this.pending.set(true);
    this.error.set(null);
    const credentials = this.credentials();
    this.authBehaviour
      .signIn({ ...credentials, username: credentials.username?.trim() ?? null })
      .subscribe({
        next: () => {
          this.pending.set(false);
          this.close();
          void this.router.navigateByUrl('/beachmap');
        },
        error: (error: unknown) => {
          this.pending.set(false);
          if (this.dialogRef && opening === this.opening) {
            this.error.set(toApiProblem(error));
          }
        },
      });
  }

  private show(): void {
    if (this.dialogRef) {
      return;
    }
    this.opening++;
    this.reset();
    this.followScreen();
    const dialogRef = this.dialog.open(this.content(), {
      width: this.width(),
      height: this.height(),
      minWidth: MIN_WIDTH,
      maxWidth: 'none',
      maxHeight: MAX_HEIGHT,
      panelClass: 'login__dialog__panel',
      ariaLabelledBy: 'login-dialog-title',
    });
    this.dialogRef = dialogRef;
    dialogRef.afterClosed().subscribe(() => this.closed(dialogRef));
  }

  /** Keeps the right size if the phone is rotated or the window resized while the popup is open. */
  private followScreen(): void {
    this.stopFollowingScreen();
    this.mobileQuery = this.window?.matchMedia?.(MOBILE_QUERY);
    this.mobileQuery?.addEventListener('change', this.resize);
  }

  private stopFollowingScreen(): void {
    this.mobileQuery?.removeEventListener('change', this.resize);
    this.mobileQuery = undefined;
  }

  private size(): LoginDialogSize {
    return this.mobileQuery?.matches ? this.sizes().mobile : this.sizes().desktop;
  }

  private width(): string {
    return `${this.size().width}vw`;
  }

  private height(): string {
    const { height } = this.size();
    return height ? `${height}vh` : '';
  }

  private reset(): void {
    this.credentials.set(new SignInRequest());
    this.credentialsForm().reset();
    this.error.set(null);
  }

  private readonly resize = (): void => {
    this.dialogRef?.updateSize(this.width(), this.height());
  };

  private closed(dialogRef: MatDialogRef<unknown>): void {
    if (this.dialogRef === dialogRef) {
      // Esc or a click outside: still the current popup.
      this.close();
    }
    if (!this.dialogRef) {
      // No new popup opened meanwhile. Emptied here too: the typed password does not stay in memory.
      this.stopFollowingScreen();
      this.reset();
    }
  }

  ngOnDestroy(): void {
    this.stopFollowingScreen();
    const dialogRef = this.dialogRef;
    this.dialogRef = undefined;
    dialogRef?.close();
  }
}
