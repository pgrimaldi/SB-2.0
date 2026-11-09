import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';

@Injectable({ providedIn: 'root' })
export class SectorService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly sectorList = (request: PageRequest): Observable<Page<SectorItem>> =>
    this.httpClient.post<Page<SectorItem>>(`${this.endpoint}/sectors-list`, request);
}
