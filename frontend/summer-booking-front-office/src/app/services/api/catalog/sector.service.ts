import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { Sector } from '../../../entities/settings/rows-and-sectors/sector';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class SectorService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly sectorList = (request: PageRequest): Observable<Page<SectorItem>> =>
    this.httpClient.post<Page<SectorItem>>(`${this.endpoint}/sectors-list`, request);

  /** Every sector of the property, in its order: `id` is the idSector, `value` the name. */
  readonly sectorCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/sectors/sector-combobox`, null);

  /** Every area of the property's map: `id` is the idSectorArea, `value` the name. */
  readonly sectorAreaCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/sectors/sector-area-combobox`, null);

  /** The backend puts the new sector at the end of the order. Answers 201 with the sector created. */
  readonly addSector = (name: string): Observable<Sector> =>
    this.httpClient.post<Sector>(`${this.endpoint}/sectors`, { name });

  /**
   * The new name of the sector, with the version of its row. Answers 200 with the sector saved and
   * the new version.
   */
  readonly updateSector = (
    idSector: string,
    name: string,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<Sector>(
        `${this.endpoint}/sectors/${encodeURIComponent(idSector)}`,
        { name },
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));

  /**
   * One sector (the trash of a row) or more (the selection), all or nothing: one id that cannot be
   * deleted and none is. Answers 204 with no body.
   */
  readonly deleteSectors = (idSectors: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/sectors/bulk-delete`, { idSectors });

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateSectors = (idSectors: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/sectors/bulk-duplicate`, { idSectors });
}
