import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MapBackgroundPanel } from '../../../components/management/settings/map-configuration/map-background-panel/map-background-panel';

@Component({
  selector: 'app-map-configuration',
  imports: [MapBackgroundPanel],
  templateUrl: './map-configuration.html',
  styleUrl: './map-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapConfiguration {}
