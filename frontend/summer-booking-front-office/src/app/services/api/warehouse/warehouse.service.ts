import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ManagementRequest } from '../../../entities/management/management-request';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/warehouse`;

  /**
   * One page of the property's articles in the chosen period.
   * An arrow property, not a method, on purpose: pages hand it to `app-table` as its loader
   * (`[load]="warehouse.list"`), and only an arrow function keeps `this` (this service) when it is
   * passed around on its own; a method would lose it and fail on `this.httpClient`.
   */
  readonly list = (request: ManagementRequest & PageRequest): Observable<Page<WarehouseItem>> =>
    this.httpClient.post<Page<WarehouseItem>>(`${this.endpoint}/list`, request);

  /** All the articles of the property for a select: id and name only, by name. */
  readonly comboboxList = (
    request: Pick<ManagementRequest, 'idProperty'>,
  ): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/combobox-list`, request);
}
