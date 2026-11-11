import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { MapBackgroundImage } from '../../../entities/enums/map-background-image';
import { MapGrid } from '../../../entities/settings/map-configuration/map-grid';
import { MapLayout } from '../../../entities/settings/map-configuration/map-layout';
import { MapSettings } from '../../../entities/settings/map-configuration/map-settings';
import { onlyFieldsOf } from '../only-fields-of';

/**
 * The property comes from the access token: only the combobox carries idProperty, like every
 * other combobox.
 */
@Injectable({ providedIn: 'root' })
export class MapService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog/map`;

  readonly settings = (): Observable<MapSettings> =>
    this.httpClient.get<MapSettings>(`${this.endpoint}/settings`);

  /** The image goes as the `file` field of a multipart form. */
  readonly uploadBackground = (image: File): Observable<MapSettings> => {
    const form = new FormData();
    form.append('file', image);
    return this.httpClient.post<MapSettings>(`${this.endpoint}/background`, form);
  };

  readonly removeBackground = (): Observable<MapSettings> =>
    this.httpClient.delete<MapSettings>(`${this.endpoint}/background`);

  /** One of the frontend's backgrounds: it replaces the uploaded one. Answers 204 with no body. */
  readonly updateBackground = (backgroundName: MapBackgroundImage): Observable<void> =>
    this.httpClient.put<void>(`${this.endpoint}/update-background`, { backgroundName });

  /** Every value of Imposta reticolo, icons included. Answers 204 with no body. */
  readonly updateConfiguration = (grid: MapGrid): Observable<void> =>
    this.httpClient.put<void>(`${this.endpoint}/update-configuration`, onlyFieldsOf(MapGrid, grid));

  /** Deletes the spots on the map and brings the grid back to 1 row of 1 item, tilt 0. */
  readonly reset = (): Observable<MapLayout> =>
    this.httpClient.post<MapLayout>(`${this.endpoint}/reset`, null);

  /** Every area of the property's map: `id` is the idSectorArea, `value` the name. */
  readonly sectorAreaCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/sector-area-combobox`, { idProperty });
}
