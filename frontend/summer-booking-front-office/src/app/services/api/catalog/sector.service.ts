import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { DeleteSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/delete-sector-items-request';
import { DuplicateSectorItemsRequest } from '../../../entities/settings/rows-and-sectors/duplicate-sector-items-request';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';

@Injectable({ providedIn: 'root' })
export class SectorService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly sectorList = (request: PageRequest): Observable<Page<SectorItem>> =>
    this.httpClient.post<Page<SectorItem>>(`${this.endpoint}/sectors-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteSectorItems = (request: DeleteSectorItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-sector-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateSectorItems = (request: DuplicateSectorItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-sector-item`, request);
}
