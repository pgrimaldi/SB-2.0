import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';
import { AlertPopup } from '../../shared/ui/dialogs/alert/alert-popup';

@Component({
  selector: 'app-management-menu',
  imports: [AlertPopup, I18nText, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './management-menu.html',
  styleUrl: './management-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementMenu {
  /** Pages of the management area; `label` is a translation key. */
  protected readonly links = [
    {
      route: '/warehouse',
      icon: '/assets/images/warehouse.svg',
      label: 'management.menu.warehouse',
    },
  ] as const;

  private readonly authBehaviour = inject(AuthBehaviour);

  /** While the server revokes the session: "Disconnetti" cannot be pressed again. */
  protected readonly loggingOut = signal(false);
  /** The server could not revoke the session: the user is still signed in and is told so. */
  protected readonly logoutFailed = signal(false);

  protected async logout(): Promise<void> {
    if (this.loggingOut()) {
      return;
    }
    this.loggingOut.set(true);
    const revoked = await this.authBehaviour.logout();
    this.loggingOut.set(false);
    this.logoutFailed.set(!revoked);
  }
}
