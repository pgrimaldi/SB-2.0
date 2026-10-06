import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { UnsavedChangesBehaviour } from '../../../behaviours/forms/unsaved-changes.behaviour';
import { I18nText } from '../../i18n/i18n-text/i18n-text';
import { AlertPopup } from '../../shared/ui/dialogs/alert-popup/alert-popup';

@Component({
  selector: 'app-management-menu',
  imports: [AlertPopup, I18nText, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './management-menu.html',
  styleUrl: './management-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementMenu {
  private readonly authBehaviour = inject(AuthBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  /** `label` is a translation key. */
  protected readonly links = [
    {
      route: '/warehouse',
      icon: '/assets/images/warehouse.svg',
      label: 'management.menu.warehouse',
    },
  ] as const;

  protected readonly loggingOut = signal(false);
  protected readonly logoutFailed = signal(false);

  protected async logout(): Promise<void> {
    if (this.loggingOut()) {
      return;
    }
    this.loggingOut.set(true);
    if (!(await this.unsaved.confirmLeave())) {
      this.loggingOut.set(false);
      return;
    }
    const revoked = await this.authBehaviour.logout();
    this.loggingOut.set(false);
    this.logoutFailed.set(!revoked);
  }
}
