import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DeleteWarehouseItemsRequest } from '../../../entities/warehouse/delete-warehouse-items-request';
import { DuplicateWarehouseItemsRequest } from '../../../entities/warehouse/duplicate-warehouse-items-request';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page } from '../../../entities/pagination/page';
import { WarehouseComboboxRequest } from '../../../entities/warehouse/warehouse-combobox-request';
import { WarehouseListRequest } from '../../../entities/warehouse/warehouse-list-request';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/warehouse`;

  /**
   * An arrow property, not a method, on purpose: pages hand it to `app-table` as its loader
   * (`[load]="warehouse.list"`), and only an arrow function keeps `this` (this service) when it is
   * passed around on its own; a method would lose it and fail on `this.httpClient`.
   */
  readonly list = (request: WarehouseListRequest): Observable<Page<WarehouseItem>> =>
    this.httpClient.post<Page<WarehouseItem>>(`${this.endpoint}/list`, request);

  /** All the articles of the property, not paged, sorted by name. */
  readonly comboboxList = (request: WarehouseComboboxRequest): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/combobox-list`, request);

  /** Answers 204 with no body. */
  readonly addWarehouseItem = (idProperty: string, item: WarehouseItem): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/add-warehouse-item`, {
      ...onlyFieldsOf(WarehouseItem, item),
      idProperty,
    });

  /** Answers 204 with no body. */
  readonly editWarehouseItem = (idProperty: string, item: WarehouseItem): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/edit-warehouse-item`, {
      ...onlyFieldsOf(WarehouseItem, item),
      idProperty,
    });

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteWarehouseItems = (request: DeleteWarehouseItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-warehouse-item`, request);

  /**
   * All or nothing, like the delete. The backend names the copies. Answers 204 with no body.
   */
  readonly duplicateWarehouseItems = (request: DuplicateWarehouseItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-warehouse-item`, request);
}
