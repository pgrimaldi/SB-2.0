import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { LoginDialog } from './login-dialog';

/** Popup size as a percentage of the screen. */
export interface LoginDialogSize {
  width: number;
  height: number;
}

/** Sizes for regular screens and for phones (below the 48rem mobile breakpoint). */
export interface LoginDialogSizes {
  desktop: LoginDialogSize;
  mobile: LoginDialogSize;
}

const DEFAULT_SIZES: LoginDialogSizes = {
  desktop: { width: 30, height: 40 },
  mobile: { width: 90, height: 55 },
};

const MOBILE_QUERY = '(width < 48rem)';

/** Opens the hub login popup (wrapper around the Angular Material dialog). */
@Injectable({ providedIn: 'root' })
export class LoginDialogService {
  private readonly dialog = inject(MatDialog);
  private readonly translateService = inject(TranslateService);
  private readonly window = inject(DOCUMENT).defaultView;

  open(sizes: LoginDialogSizes = DEFAULT_SIZES): MatDialogRef<LoginDialog> {
    const mobile = this.window?.matchMedia?.(MOBILE_QUERY);
    const size = () => (mobile?.matches ? sizes.mobile : sizes.desktop);

    const dialogRef = this.dialog.open(LoginDialog, {
      width: `${size().width}vw`,
      height: `${size().height}vh`,
      maxWidth: 'none',
      panelClass: 'login__dialog__panel',
      ariaLabel: this.translateService.instant('hub.header.login'),
    });

    // Keep the right size if the phone is rotated or the window resized while the popup is open.
    const resize = () => dialogRef.updateSize(`${size().width}vw`, `${size().height}vh`);
    mobile?.addEventListener('change', resize);
    dialogRef.afterClosed().subscribe(() => mobile?.removeEventListener('change', resize));

    return dialogRef;
  }
}
