import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { SaveSectorRequest } from '../../../entities/settings/rows-and-sectors/save-sector-request';
import { Sector } from '../../../entities/settings/rows-and-sectors/sector';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class SectorService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly sectorList = (request: PageRequest): Observable<Page<SectorItem>> =>
    this.httpClient.post<Page<SectorItem>>(`${this.endpoint}/sectors-list`, request);

  /** Every sector of the property, in its order: `id` is the idSector, `value` the name. */
  readonly sectorCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/sector-combobox`, null);

  /** The backend puts the new sector at the end of the order. Answers 201 with the sector created. */
  readonly addSector = (request: SaveSectorRequest): Observable<Sector> =>
    this.httpClient.post<Sector>(
      `${this.endpoint}/sectors`,
      onlyFieldsOf(SaveSectorRequest, request),
    );

  /** The new name of the sector. Answers 200 with the sector saved. */
  readonly updateSector = (idSector: string, request: SaveSectorRequest): Observable<Sector> =>
    this.httpClient.put<Sector>(
      `${this.endpoint}/sectors/${encodeURIComponent(idSector)}`,
      onlyFieldsOf(SaveSectorRequest, request),
    );

  /** Answers 204 with no body. */
  readonly deleteSector = (idSector: string): Observable<void> =>
    this.httpClient.delete<void>(`${this.endpoint}/sectors/${encodeURIComponent(idSector)}`);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteSectors = (idSectors: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/sectors/bulk-delete`, { idSectors });

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateSectors = (idSectors: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/sectors/bulk-duplicate`, { idSectors });
}
