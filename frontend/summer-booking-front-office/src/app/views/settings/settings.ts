import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SettingsMenu } from '../../components/settings/menu/settings-menu';

/**
 * Settings of the property (the gear of the management header): the side panel with the settings
 * pages and, next to it, the page chosen (`views/settings/<page>`, children of the `settings` route).
 */
@Component({
  selector: 'app-settings',
  imports: [RouterOutlet, SettingsMenu],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings {}
