import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SettingsLink, SettingsMenu } from '../../components/settings/menu/settings-menu';
import { SETTINGS_PAGES } from './settings-pages';

@Component({
  selector: 'app-settings',
  imports: [RouterOutlet, SettingsMenu],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings {
  protected readonly links: readonly SettingsLink[] = SETTINGS_PAGES.map(({ path, label }) => ({
    route: `/settings/${path}`,
    label,
  }));
}
