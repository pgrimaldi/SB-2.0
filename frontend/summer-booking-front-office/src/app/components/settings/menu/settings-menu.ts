import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { I18nText } from '../../i18n/i18n-text/i18n-text';

/** `label` is a translation key. */
export interface SettingsLink {
  route: string;
  label: string;
}

@Component({
  selector: 'app-settings-menu',
  imports: [I18nText, RouterLink, RouterLinkActive],
  templateUrl: './settings-menu.html',
  styleUrl: './settings-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsMenu {
  readonly links = input.required<readonly SettingsLink[]>();
}
