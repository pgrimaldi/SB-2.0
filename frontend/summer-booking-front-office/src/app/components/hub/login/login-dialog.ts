import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';

/** Content of the hub login popup (empty for now: fields and texts come next). */
@Component({
  selector: 'app-login-dialog',
  template: '',
  styleUrl: './login-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The dialog panel lives in the overlay, outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class LoginDialog {}
