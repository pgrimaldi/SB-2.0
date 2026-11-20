import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { WarehouseItemAddRequest } from '../../../entities/warehouse/warehouse-item-add-request';
import { WarehouseItemUpdateRequest } from '../../../entities/warehouse/warehouse-item-update-request';
import { WarehouseSelectedItem } from '../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../entities/warehouse/warehouse-setting-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/warehouse`;
  private readonly catalogEndpoint = `${environment.apiBaseUrl}/catalog`;

  /**
   * An arrow property, not a method, on purpose: pages hand it to `app-table` as its loader
   * (`[load]="warehouse.list"`), and only an arrow function keeps `this` (this service) when it is
   * passed around on its own; a method would lose it and fail on `this.httpClient`.
   */
  readonly list = (request: PageRequest): Observable<Page<WarehouseItem>> =>
    this.httpClient.post<Page<WarehouseItem>>(`${this.endpoint}/list`, request);

  /** The table of Impostazioni magazzino, an arrow property like `list`. */
  readonly inventoryItemsList = (request: PageRequest): Observable<Page<WarehouseSettingItem>> =>
    this.httpClient.post<Page<WarehouseSettingItem>>(
      `${this.catalogEndpoint}/inventory-items-list`,
      request,
    );

  /** All the articles of the property, not paged, by name in natural order. */
  readonly articleCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.catalogEndpoint}/article-combobox`, null);

  /** Adds pieces to an article; a null threshold or alert stays as it is. Answers 204 with no body. */
  readonly addWarehouseItem = (request: WarehouseItemAddRequest): Observable<void> =>
    this.httpClient.post<void>(
      `${this.catalogEndpoint}/add-warehouse-item`,
      onlyFieldsOf(WarehouseItemAddRequest, request),
    );

  /** The whole article as the backend keeps it: "Modifica articolo" reads it before editing. */
  readonly selectedArticle = (idArticle: string): Observable<WarehouseSelectedItem> =>
    this.httpClient.get<WarehouseSelectedItem>(
      `${this.catalogEndpoint}/inventory-items/${encodeURIComponent(idArticle)}`,
    );

  /** Replaces the whole article: every field read goes back, with the changes. */
  readonly updateArticle = (
    idArticle: string,
    request: WarehouseItemUpdateRequest,
  ): Observable<WarehouseSelectedItem> =>
    this.httpClient.put<WarehouseSelectedItem>(
      `${this.catalogEndpoint}/edit-warehouse-item/${encodeURIComponent(idArticle)}`,
      onlyFieldsOf(WarehouseItemUpdateRequest, request),
    );

  /**
   * One article (the trash of a row) or more (the selection), all or nothing: one id that cannot be
   * deleted and none is. Answers 204 with no body.
   */
  readonly deleteArticles = (idItems: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.catalogEndpoint}/delete-warehouse-item`, { idItems });

  /**
   * One article (the duplicate of a row) or more (the selection), all or nothing like the delete.
   * The backend names the copies. Answers 204 with no body.
   */
  readonly duplicateWarehouseItems = (idItems: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.catalogEndpoint}/duplicate-warehouse-item`, { idItems });
}
