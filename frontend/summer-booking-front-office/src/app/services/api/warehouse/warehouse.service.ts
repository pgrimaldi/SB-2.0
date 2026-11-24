import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { WarehouseItemAddRequest } from '../../../entities/warehouse/warehouse-item-add-request';
import { WarehouseItemUpdateRequest } from '../../../entities/warehouse/warehouse-item-update-request';
import { WarehouseSelectedItem } from '../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../entities/warehouse/warehouse-setting-item';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

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

  readonly selectedArticle = (idArticle: string): Observable<WarehouseSelectedItem> =>
    this.httpClient.get<WarehouseSelectedItem>(
      `${this.catalogEndpoint}/inventory-items/${encodeURIComponent(idArticle)}`,
    );

  /**
   * Replaces the whole article: every field read goes back, with the changes, and the version read.
   * Answers the new version.
   */
  readonly updateArticle = (
    idArticle: string,
    request: WarehouseItemUpdateRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<WarehouseSelectedItem>(
        `${this.catalogEndpoint}/edit-warehouse-item/${encodeURIComponent(idArticle)}`,
        onlyFieldsOf(WarehouseItemUpdateRequest, request),
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));

  /**
   * One article (the trash of a row) or more (the selection), all or nothing: one id that cannot be
   * deleted and none is. Answers 204 with no body.
   */
  readonly deleteArticles = (idItems: string[]): Observable<void> =>
    this.httpClient
      .post<void>(`${this.catalogEndpoint}/delete-warehouse-item`, { idItems })
      .pipe(catchError((error: unknown) => throwError(() => withUsedIn(error))));

  /**
   * One article (the duplicate of a row) or more (the selection), all or nothing like the delete.
   * The backend names the copies. Answers 204 with no body.
   */
  readonly duplicateWarehouseItems = (idItems: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.catalogEndpoint}/duplicate-warehouse-item`, { idItems });
}

/** `inventory.item_in_use` with `usedIn`: the price lists and the agreement price lists together. */
function withUsedIn(error: unknown): unknown {
  if (!(error instanceof HttpErrorResponse)) {
    return error;
  }
  const body: unknown = error.error;
  if (
    typeof body !== 'object' ||
    body === null ||
    !('code' in body) ||
    body.code !== ApiErrorCode.InventoryItemInUse
  ) {
    return error;
  }
  const args: Record<string, unknown> =
    'args' in body && typeof body.args === 'object' && body.args !== null
      ? { ...(body.args as Record<string, unknown>) }
      : {};
  const usedIn = [args['priceLists'], args['agreementPriceLists']].flatMap((names) =>
    Array.isArray(names) ? names : [],
  );
  return new HttpErrorResponse({
    error: { ...body, args: { ...args, usedIn } },
    headers: error.headers,
    status: error.status,
    statusText: error.statusText,
    url: error.url ?? undefined,
  });
}
