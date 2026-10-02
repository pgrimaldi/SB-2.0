import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { I18nText } from '../../i18n/i18n-text/i18n-text';

/** A page of the settings: its route and the translation key of its name. */
export interface SettingsLink {
  route: string;
  label: string;
}

/**
 * Side panel of the settings pages: the list of the settings pages (`links`, given by the page), the
 * current one highlighted. Next to the management menu on desktop; a scrollable strip above the page
 * on tablets and phones. `<app-settings-menu [links]="links" />`.
 */
@Component({
  selector: 'app-settings-menu',
  imports: [I18nText, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './settings-menu.html',
  styleUrl: './settings-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsMenu {
  /** Pages of the settings, in menu order. */
  readonly links = input.required<readonly SettingsLink[]>();
}
