import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ManagementRequest } from '../../../entities/management/management-request';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/warehouse`;

  /** One page of the property's articles in the chosen period; `search` is looked for in the name. */
  list(request: ManagementRequest & PageRequest): Observable<Page<WarehouseItem>> {
    return this.httpClient.post<Page<WarehouseItem>>(`${this.endpoint}/list`, request);
  }
}
