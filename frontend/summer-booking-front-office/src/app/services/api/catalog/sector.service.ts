import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/delete-sector-items-request';
import { DuplicateSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/duplicate-sector-items-request';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class SectorService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly sectorList = (request: PageRequest): Observable<Page<SectorItem>> =>
    this.httpClient.post<Page<SectorItem>>(`${this.endpoint}/sectors-list`, request);

  /** Every sector of the property, in its order: `id` is the idSector, `value` the name. */
  readonly sectorCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/sector-combobox`, { idProperty });

  /** Only the name: the backend puts the new sector at the end of the order. Answers 204 with no body. */
  readonly addSectorItem = (idProperty: string, sector: SectorItem): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-sector-item`, {
      idProperty,
      sectorName: sector.sectorName,
    });

  /** The new name of the sector. Answers 204 with no body. */
  readonly updateSectorItem = (idProperty: string, sector: SectorItem): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-sector-item`, {
      ...onlyFieldsOf(SectorItem, sector),
      idProperty,
    });

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteSectorItems = (request: DeleteSectorItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-sector-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateSectorItems = (request: DuplicateSectorItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-sector-item`, request);
}
