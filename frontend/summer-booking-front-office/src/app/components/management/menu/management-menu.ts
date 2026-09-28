import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';

@Component({
  selector: 'app-management-menu',
  imports: [I18nText, RouterLink, RouterLinkActive, TranslatePipe],
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

  protected logout(): void {
    this.authBehaviour.logout();
  }
}
