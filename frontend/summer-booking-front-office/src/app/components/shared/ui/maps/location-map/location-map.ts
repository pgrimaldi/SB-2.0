import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { GoogleMap, MapAdvancedMarker } from '@angular/google-maps';

export interface MapPosition {
  latitude: number;
  longitude: number;
}

// Google's script must already be on the page: who uses the wrapper loads it first.
@Component({
  selector: 'app-location-map',
  imports: [GoogleMap, MapAdvancedMarker],
  templateUrl: './location-map.html',
  styleUrl: './location-map.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationMap {
  readonly center = input.required<MapPosition>();
  readonly zoom = input.required<number>();
  /** Needed by the marker: Google draws it only on a map with an id. */
  readonly mapId = input<string | null>();
  readonly markerPosition = input<MapPosition | null>();

  protected readonly options: google.maps.MapOptions = {
    disableDefaultUI: true,
    fullscreenControl: true,
    clickableIcons: false,
  };
  protected readonly googleCenter = computed(() => toLatLng(this.center()));
  protected readonly googleMarkerPosition = computed(() => {
    const position = this.markerPosition();
    return position ? toLatLng(position) : null;
  });
}

function toLatLng({ latitude, longitude }: MapPosition): google.maps.LatLngLiteral {
  return { lat: latitude, lng: longitude };
}
