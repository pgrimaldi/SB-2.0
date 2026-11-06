import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MapBackgroundPanel } from '../../../components/management/settings/map-configuration/map-background-panel/map-background-panel';
import { MapGridPanel } from '../../../components/management/settings/map-configuration/map-grid-panel/map-grid-panel';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../components/shared/ui/buttons/button/button';
import { WarningPopup } from '../../../components/shared/ui/dialogs/warning-popup/warning-popup';

@Component({
  selector: 'app-map-configuration',
  imports: [Button, MapBackgroundPanel, MapGridPanel, TranslatePipe, WarningPopup],
  templateUrl: './map-configuration.html',
  styleUrl: './map-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapConfiguration {
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly resetIcon = ['/assets/images/MapConfiguration_Reset.svg'] as const;
  /** [X, warning]. */
  protected readonly resetPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/MapConfiguration_Warning.svg',
  ] as const;
  protected readonly isResetAsked = signal(false);
}
