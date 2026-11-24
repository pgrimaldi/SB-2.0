import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { MapBackgroundImage } from '../../../entities/enums/map-background-image';
import { MapCatalogIcon } from '../../../entities/settings/map-configuration/map-catalog-icon';
import { MapGrid } from '../../../entities/settings/map-configuration/map-grid';
import { MapLayout } from '../../../entities/settings/map-configuration/map-layout';
import { MapSettings } from '../../../entities/settings/map-configuration/map-settings';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class MapService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog/map`;

  readonly settings = (): Observable<MapSettings> =>
    this.httpClient.get<MapSettings>(`${this.endpoint}/settings`);

  /** The active icons of the catalogue of the map, backgrounds included. */
  readonly iconCatalog = (): Observable<MapCatalogIcon[]> =>
    this.httpClient.get<MapCatalogIcon[]>(`${this.endpoint}/icon-catalog`);

  /** The image goes as the `file` field of a multipart form. */
  readonly uploadBackground = (image: File): Observable<MapSettings> => {
    const form = new FormData();
    form.append('file', image);
    return this.httpClient.post<MapSettings>(`${this.endpoint}/background`, form);
  };

  readonly removeBackground = (): Observable<MapSettings> =>
    this.httpClient.delete<MapSettings>(`${this.endpoint}/background`);

  /**
   * One of the frontend's backgrounds, with `backgroundVersion`: it replaces the uploaded one.
   * Answers 204 with no body and the new `backgroundVersion`.
   */
  readonly updateBackground = (
    backgroundName: MapBackgroundImage,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<void>(
        `${this.endpoint}/update-background`,
        { backgroundName },
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));

  /**
   * Every value of Imposta reticolo, icons included, with `configurationVersion`. Answers 204 with no
   * body and the new `configurationVersion`.
   */
  readonly updateConfiguration = (
    grid: MapGrid,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<void>(`${this.endpoint}/update-configuration`, onlyFieldsOf(MapGrid, grid), {
        headers: ifMatch(version),
        observe: 'response',
      })
      .pipe(map(versionOf));

  /** Deletes the spots on the map and empties the grid (0 × 0); background, icons and tilt stay. */
  readonly reset = (): Observable<MapLayout> =>
    this.httpClient.post<MapLayout>(`${this.endpoint}/reset`, null);
}
