import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_SEARCH_MAX_LENGTH,
} from '../../../components/shared/ui/tables/table/table';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import {
  DEFAULT_PAGE_SIZE,
  Page,
  PageRequest,
  SortDirection,
} from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { WarehouseItemAddRequest } from '../../../entities/warehouse/warehouse-item-add-request';
import { MAX_WAREHOUSE_QUANTITY } from '../../../entities/warehouse/warehouse-limits';
import { WarehouseItemUpdateRequest } from '../../../entities/warehouse/warehouse-item-update-request';
import { WarehouseSelectedItem } from '../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../entities/warehouse/warehouse-setting-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import { sameText, sameTextIgnoringCase } from '../../../components/shared/forms/text-comparison';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

/**
 * Rows of `catalog.inventory_items`: same structure as the sample DB, invented values (no data of
 * real properties). Each article belongs to one property (`propertyId` → `MOCK_PROPERTIES.id`):
 * a request only ever sees the articles of its own property.
 */
const INVENTORY_ITEMS: InventoryRow[] = [
  {
    id: 1,
    publicId: '276a67d2-d0eb-47f5-8223-4804b9a9dcf9',
    propertyId: 1,
    articleCode: 'umbrella',
    name: 'Ombrellone',
    mapPlaceable: true,
    usage: {
      priceLists: ['Ombrellone e lettino', 'Ombrellone e 2 lettini'],
      agreementPriceLists: [],
    },
    totalQuantity: 60,
    lowStockThreshold: 3,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 2,
    publicId: 'c87770a7-74b6-4dc4-a5ad-e193e136bacb',
    propertyId: 1,
    articleCode: 'sunbed',
    name: 'Lettino',
    mapPlaceable: true,
    usage: {
      priceLists: ['Ombrellone e 2 lettini'],
      agreementPriceLists: ['Hotel Mare – Listino convenzione'],
    },
    totalQuantity: 120,
    lowStockThreshold: 5,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 3,
    publicId: '62fbf392-641d-4b01-9776-70e76c0cbaec',
    propertyId: 1,
    articleCode: 'deckchair',
    name: 'Sdraio',
    mapPlaceable: false,
    totalQuantity: 80,
    lowStockThreshold: 4,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 4,
    publicId: 'f58474cb-a7b7-47c0-99ba-cd2cb30d28a0',
    propertyId: 1,
    articleCode: 'cabin',
    name: 'Cabina',
    mapPlaceable: true,
    totalQuantity: 24,
    lowStockThreshold: 2,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 5,
    publicId: '94072a53-679f-4293-bce5-f6b677722826',
    propertyId: 1,
    articleCode: null,
    name: 'Doccia',
    mapPlaceable: false,
    totalQuantity: 8,
    lowStockThreshold: null,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 6,
    publicId: '9eb8eed7-189f-4931-84fa-8e9a8f9e2502',
    propertyId: 1,
    articleCode: null,
    name: 'Spogliatoio',
    mapPlaceable: false,
    totalQuantity: 4,
    lowStockThreshold: 1,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 7,
    publicId: 'd1d37a6c-7fc5-42dc-ad1b-6b95289bc868',
    propertyId: 1,
    articleCode: 'parking',
    name: 'Parcheggio',
    mapPlaceable: true,
    totalQuantity: 35,
    lowStockThreshold: null,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 8,
    publicId: 'afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62',
    propertyId: 1,
    articleCode: 'sunbed_xl',
    name: 'Lettino XL',
    mapPlaceable: true,
    totalQuantity: 16,
    lowStockThreshold: 2,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 9,
    publicId: '98fe5970-0b1b-45bf-a889-36c9887dd0a6',
    propertyId: 1,
    articleCode: 'directors_chair',
    name: 'Sedia regista',
    mapPlaceable: false,
    totalQuantity: 30,
    lowStockThreshold: 3,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 10,
    publicId: '2049d281-27d1-4e06-befa-ccb6ea8a245e',
    propertyId: 1,
    articleCode: null,
    name: 'Ombrellone grande',
    mapPlaceable: false,
    totalQuantity: 12,
    lowStockThreshold: 1,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 11,
    publicId: '2ae86856-ad2a-4dcd-81c7-fc77a97ee053',
    propertyId: 1,
    articleCode: 'tent',
    name: 'Tenda',
    mapPlaceable: true,
    totalQuantity: 10,
    lowStockThreshold: 1,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 12,
    publicId: 'de7c776e-9353-42d6-8035-4839dc6e6f9f',
    propertyId: 1,
    articleCode: null,
    name: 'Tavolino',
    mapPlaceable: false,
    totalQuantity: 25,
    lowStockThreshold: null,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 13,
    publicId: 'ab6add1e-605e-4ff4-94fb-338bcf60240e',
    propertyId: 1,
    articleCode: 'umbrella',
    name: 'Ombrellone vecchio',
    mapPlaceable: true,
    totalQuantity: 5,
    lowStockThreshold: 1,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
  {
    id: 14,
    publicId: '62bf4a2c-db53-4140-b885-4f9c62ae40d4',
    propertyId: 2,
    articleCode: null,
    name: 'Pedalò',
    mapPlaceable: false,
    totalQuantity: 6,
    lowStockThreshold: 1,
    lowStockAlertEnabled: true,
    deletedAt: null,
  },
  {
    id: 15,
    publicId: '43fb6129-4be6-46e0-905e-f42e14425c43',
    propertyId: 2,
    articleCode: null,
    name: 'Canoa',
    mapPlaceable: false,
    totalQuantity: 4,
    lowStockThreshold: 1,
    lowStockAlertEnabled: false,
    deletedAt: null,
  },
];

type ConsumptionState = 'held' | 'confirmed' | 'released';

/** Rows of `booking.resource_consumptions` for today: same structure as the sample DB, invented values. */
const TODAY_CONSUMPTIONS: { resourceId: string; quantity: number; state: ConsumptionState }[] = [
  { resourceId: '276a67d2-d0eb-47f5-8223-4804b9a9dcf9', quantity: 15, state: 'confirmed' },
  { resourceId: 'c87770a7-74b6-4dc4-a5ad-e193e136bacb', quantity: 30, state: 'confirmed' },
  { resourceId: 'c87770a7-74b6-4dc4-a5ad-e193e136bacb', quantity: 6, state: 'held' },
  { resourceId: '62fbf392-641d-4b01-9776-70e76c0cbaec', quantity: 10, state: 'confirmed' },
  { resourceId: 'f58474cb-a7b7-47c0-99ba-cd2cb30d28a0', quantity: 5, state: 'released' },
  { resourceId: 'd1d37a6c-7fc5-42dc-ad1b-6b95289bc868', quantity: 7, state: 'confirmed' },
  { resourceId: '2ae86856-ad2a-4dcd-81c7-fc77a97ee053', quantity: 3, state: 'held' },
  { resourceId: '62bf4a2c-db53-4140-b885-4f9c62ae40d4', quantity: 2, state: 'confirmed' },
];

/**
 * `POST /api/catalog/article-combobox` without a body: the active articles of the property (never
 * of other properties), id and name only, by name in natural order (Gazebo 2 before Gazebo 10),
 * then id. 401 without a valid access token.
 */
export const articleComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const items: ComboboxItem[] = INVENTORY_ITEMS.filter(
    (item) => item.propertyId === property.id && item.deletedAt === null,
  )
    .sort(
      (first, second) =>
        first.name
          .trim()
          .localeCompare(second.name.trim(), 'it', { sensitivity: 'base', numeric: true }) ||
        first.id - second.id,
    )
    .map((item) => ({ id: item.publicId, value: item.name }));
  return of(new HttpResponse({ status: 200, url: request.url, body: items })).pipe(delay(150));
};

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * `POST /api/catalog/add-warehouse-item`: adds `totalQuantity` pieces to the total of an article that
 * exists already; a null threshold or alert stays as it is (until the page is reloaded). 204 without
 * a body. 401 without a valid access token; 400 without `errors[]` for an `idArticle` that is not a
 * UUID (the backend cannot read it); 400 with one error per wrong field (see `addErrors`), also a new
 * total over the limit or a threshold over it; 404 `inventory.item_not_found` for an article that is
 * not an active one of the property.
 */
export const warehouseAddMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<WarehouseItemAddRequest>;
  if (typeof body.idArticle === 'string' && body.idArticle && !UUID.test(body.idArticle)) {
    return problem(request, 400, ApiErrorCode.ValidationFailed);
  }
  const errors = addErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const item = articleOf(DEMO_PROPERTY.id, body.idArticle);
  if (!item) {
    return problem(request, 404, ApiErrorCode.InventoryItemNotFound);
  }
  const total = item.totalQuantity + (body.totalQuantity as number);
  const threshold = body.lowStockThreshold ?? null;
  const totalErrors: MockFieldError[] = [
    ...(total > MAX_WAREHOUSE_QUANTITY
      ? [{ field: 'totalQuantity', code: ApiErrorCode.ValidationOutOfRange }]
      : []),
    ...(threshold !== null && threshold > total
      ? [{ field: 'lowStockThreshold', code: ApiErrorCode.ValidationOutOfRange }]
      : []),
  ];
  if (totalErrors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors: totalErrors });
  }

  item.totalQuantity = total;
  if (threshold !== null) {
    item.lowStockThreshold = threshold;
  }
  if (typeof body.lowStockAlertEnabled === 'boolean') {
    item.lowStockAlertEnabled = body.lowStockAlertEnabled;
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** The rules of the body alone, one error per wrong field, as the backend answers them. */
function addErrors(body: Partial<WarehouseItemAddRequest>): MockFieldError[] {
  const { totalQuantity, lowStockAlertEnabled } = body;
  const threshold = body.lowStockThreshold ?? null;
  const errors: MockFieldError[] = [];
  if (typeof body.idArticle !== 'string' || !body.idArticle) {
    errors.push({ field: 'idArticle', code: ApiErrorCode.ValidationRequired });
  }
  if (totalQuantity === undefined || totalQuantity === null) {
    errors.push({ field: 'totalQuantity', code: ApiErrorCode.ValidationRequired });
  } else if (!Number.isInteger(totalQuantity) || totalQuantity <= 0) {
    errors.push({ field: 'totalQuantity', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (threshold !== null && !wholeNumber(threshold)) {
    errors.push({ field: 'lowStockThreshold', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (
    (lowStockAlertEnabled !== undefined &&
      lowStockAlertEnabled !== null &&
      typeof lowStockAlertEnabled !== 'boolean') ||
    (lowStockAlertEnabled === true && !(threshold !== null && threshold > 0))
  ) {
    errors.push({ field: 'lowStockAlertEnabled', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

/** Characters of the name of an article at most (`InventoryItem.NameMaxLength`). */
const MAX_ARTICLE_NAME_LENGTH = 120;

/**
 * `GET /api/catalog/inventory-items/{id}`: the whole article (`InventoryItemDto`). 401 without a
 * valid access token; 404 `inventory.item_not_found` for an article that is not an active one of the
 * property.
 */
export const selectedArticleMock = (
  request: HttpRequest<unknown>,
  idArticle: string,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const item = articleOf(DEMO_PROPERTY.id, idArticle);
  if (!item) {
    return problem(request, 404, ApiErrorCode.InventoryItemNotFound);
  }
  return of(new HttpResponse({ status: 200, url: request.url, body: selectedOf(item) })).pipe(
    delay(300),
  );
};

/**
 * `PUT /api/catalog/edit-warehouse-item/{id}` with `SaveInventoryItemRequest`: replaces the whole article
 * (until the page is reloaded) and answers 200 with it. 401 without a valid access token; 400 with
 * one error per wrong field (see `updateErrors`); 404 `inventory.item_not_found`; 409
 * `inventory.name_taken` for a name another article of the property has, ignoring case.
 */
export const updateArticleMock = (
  request: HttpRequest<unknown>,
  idArticle: string,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<WarehouseItemUpdateRequest>;
  const errors = updateErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const item = articleOf(DEMO_PROPERTY.id, idArticle);
  if (!item) {
    return problem(request, 404, ApiErrorCode.InventoryItemNotFound);
  }
  const conflict = versionConflict(request, articleData(item));
  if (conflict) {
    return conflict;
  }
  const name = String(body.name).trim();
  const isNameTaken = INVENTORY_ITEMS.some(
    (row) =>
      row !== item &&
      row.propertyId === item.propertyId &&
      row.deletedAt === null &&
      sameTextIgnoringCase(row.name, name),
  );
  if (isNameTaken) {
    return problem(request, 409, ApiErrorCode.InventoryNameTaken);
  }

  item.articleCode = body.articleCode ?? null;
  item.name = name;
  item.totalQuantity = body.totalQuantity as number;
  item.lowStockThreshold = body.lowStockThreshold ?? null;
  item.lowStockAlertEnabled = body.lowStockAlertEnabled as boolean;
  return savedWithVersion(request, articleData(item), selectedOf(item));
};

function selectedOf(item: InventoryRow): WarehouseSelectedItem {
  const article = articleData(item);
  return { ...article, version: versionOf(article) };
}

/** What `GET /inventory-items/{id}` answers besides the version: the version covers it. */
function articleData(item: InventoryRow): Omit<WarehouseSelectedItem, 'version'> {
  return {
    id: item.publicId,
    articleCode: item.articleCode,
    name: item.name,
    totalQuantity: item.totalQuantity,
    lowStockThreshold: item.lowStockThreshold,
    lowStockAlertEnabled: item.lowStockAlertEnabled,
    mapPlaceable: item.mapPlaceable,
  };
}

/**
 * The rules of `SaveInventoryItemRequestValidator` with the alert rule asked to the backend (on only
 * with a threshold above 0), one error per wrong field.
 */
function updateErrors(body: Partial<WarehouseItemUpdateRequest>): MockFieldError[] {
  const { articleCode, name, totalQuantity, lowStockAlertEnabled } = body;
  const threshold = body.lowStockThreshold ?? null;
  const errors: MockFieldError[] = [];
  if (articleCode !== undefined && articleCode !== null && typeof articleCode !== 'string') {
    errors.push({ field: 'articleCode', code: ApiErrorCode.ValidationInvalid });
  }
  if (typeof name !== 'string' || !name.trim()) {
    errors.push({ field: 'name', code: ApiErrorCode.ValidationRequired });
  } else if (name.length > MAX_ARTICLE_NAME_LENGTH) {
    errors.push({
      field: 'name',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: MAX_ARTICLE_NAME_LENGTH, min: 0 },
    });
  }
  if (!wholeNumber(totalQuantity)) {
    errors.push({ field: 'totalQuantity', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (
    threshold !== null &&
    (!wholeNumber(threshold) || (wholeNumber(totalQuantity) && threshold > totalQuantity))
  ) {
    errors.push({ field: 'lowStockThreshold', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (
    typeof lowStockAlertEnabled !== 'boolean' ||
    (lowStockAlertEnabled && !(threshold !== null && threshold > 0))
  ) {
    errors.push({ field: 'lowStockAlertEnabled', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

/**
 * `POST /api/catalog/delete-warehouse-item` with `{ idItems }`, one id or more: all or nothing. 400
 * `validation.required` without ids; 404 `inventory.item_not_found` when one id is not an active
 * article of the property; 409 `inventory.item_in_use` for the first article in use, with its
 * `articleName`; nothing is deleted then. Otherwise every row gets `deletedAt`; 204 without a body.
 * Repeated ids count once; 401 without a valid access token.
 */
export const deleteArticlesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idItems = (request.body as { idItems?: unknown } | null)?.idItems;
  if (!Array.isArray(idItems) || !idItems.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'idItems', code: ApiErrorCode.ValidationRequired }],
    });
  }
  const items = itemsOf(DEMO_PROPERTY.id, idItems);
  if (!items) {
    return problem(request, 404, ApiErrorCode.InventoryItemNotFound);
  }
  const inUse = items.find((item) => item.usage);
  if (inUse?.usage) {
    return problem(request, 409, ApiErrorCode.InventoryItemInUse, {
      args: { articleName: inUse.name, ...inUse.usage },
    });
  }

  const deletedAt = new Date().toISOString();
  for (const item of items) {
    item.deletedAt = deletedAt;
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-warehouse-item` with `{ idItems }`, one id or more: all or nothing.
 * 400 `validation.required` without ids; 404 `inventory.item_not_found` when one id is not an
 * active article of the property, and nothing is copied. Each copy keeps code, total, threshold and
 * alert and is in no price list; 204 without a body. Repeated ids count once; 401 without a valid
 * access token.
 */
export const warehouseDuplicateMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idItems = (request.body as { idItems?: unknown } | null)?.idItems;
  if (!Array.isArray(idItems) || !idItems.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'idItems', code: ApiErrorCode.ValidationRequired }],
    });
  }
  const items = itemsOf(DEMO_PROPERTY.id, idItems);
  if (!items) {
    return problem(request, 404, ApiErrorCode.InventoryItemNotFound);
  }

  for (const item of items) {
    INVENTORY_ITEMS.push({
      id: Math.max(...INVENTORY_ITEMS.map((row) => row.id)) + 1,
      publicId: crypto.randomUUID(),
      propertyId: item.propertyId,
      articleCode: item.articleCode,
      name: copyName(item),
      mapPlaceable: item.mapPlaceable,
      totalQuantity: item.totalQuantity,
      lowStockThreshold: item.lowStockThreshold,
      lowStockAlertEnabled: item.lowStockAlertEnabled,
      deletedAt: null,
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** The id of the active article of the property with this name, null when there is none. */
export function activeArticleIdNamed(propertyId: number, name: string | null): string | null {
  return (
    INVENTORY_ITEMS.find(
      (row) =>
        name !== null &&
        sameText(row.name, name) &&
        row.propertyId === propertyId &&
        row.deletedAt === null,
    )?.publicId ?? null
  );
}

/** The name of an active article of the property, null when it is not one. */
export function activeArticleName(propertyId: number, idArticle: unknown): string | null {
  return articleOf(propertyId, idArticle)?.name ?? null;
}

function articleOf(propertyId: number, idArticle: unknown): InventoryRow | undefined {
  return INVENTORY_ITEMS.find(
    (row) => row.publicId === idArticle && row.propertyId === propertyId && row.deletedAt === null,
  );
}

/**
 * The active articles of the property for `idItems` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function itemsOf(propertyId: number, idItems: unknown): InventoryRow[] | null {
  const ids = Array.isArray(idItems) ? [...new Set(idItems)] : [];
  const items = ids
    .map((id) => articleOf(propertyId, id))
    .filter((item): item is InventoryRow => item !== undefined);
  return ids.length && items.length === ids.length ? items : null;
}

const COPY_SUFFIX = / copia(?: \d{1,9})?$/i;

/**
 * Like `CopyNames.Next` of the backend: "Lettino copia", then "Lettino copia 2", the first name no
 * active article of the property has (ignoring case); a copy of a copy starts again from "Lettino".
 * A long name loses the end of its base, never of the suffix.
 */
function copyName(item: InventoryRow): string {
  const taken = new Set(
    INVENTORY_ITEMS.filter(
      (row) => row.propertyId === item.propertyId && row.deletedAt === null,
    ).map((row) => row.name.trim().toLowerCase()),
  );
  const baseName = item.name.trim().replace(COPY_SUFFIX, '');
  for (let number = 1; ; number++) {
    const suffix = number === 1 ? ' copia' : ` copia ${number}`;
    const head =
      baseName.length + suffix.length > MAX_ARTICLE_NAME_LENGTH
        ? baseName.slice(0, Math.max(0, MAX_ARTICLE_NAME_LENGTH - suffix.length)).trimEnd()
        : baseName;
    if (!taken.has(`${head}${suffix}`.toLowerCase())) {
      return `${head}${suffix}`;
    }
  }
}

const MAX_PAGE_SIZE = 100;

/**
 * `POST /api/warehouse/list`: one page of the active articles of the property of the token (never
 * of other properties) with today's availability; released consumptions free the stock. The filters
 * are not read: the invented consumptions are all for today. 401 without a valid access token.
 */
export const warehouseMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);

  const items: WarehouseItem[] = INVENTORY_ITEMS.filter(
    (item) => item.propertyId === property.id && item.deletedAt === null,
  ).map((item) => {
    const consumedQuantity = TODAY_CONSUMPTIONS.filter(
      (consumption) => consumption.resourceId === item.publicId && consumption.state !== 'released',
    ).reduce((sum, consumption) => sum + consumption.quantity, 0);

    return {
      idArticle: item.publicId,
      name: item.name,
      totalQuantity: item.totalQuantity,
      availableQuantity: item.totalQuantity - consumedQuantity,
      thresholdQuantity: item.lowStockThreshold,
      isThresholdWarningActive: item.lowStockAlertEnabled,
    };
  });

  // The server decides where to search: the mock looks in the article name, ignoring case.
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = search
    ? items.filter((item) => item.name?.trim().toLowerCase().includes(search))
    : items;
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }

  const answer: Page<WarehouseItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

const SORT_FIELDS = ['name', 'totalQuantity', 'availableQuantity', 'thresholdQuantity'] as const;

/**
 * `POST /api/catalog/inventory-items-list`: one page of the active articles of the property of the
 * token, by name in natural order unless sorted; without a threshold always last when sorted by
 * threshold; ties by name, then id. 400 `validation.invalid` on `page`, `pageSize`, `sortField`,
 * `sortDirection` or `filters` out of the table's rules and `validation.max_length` on a `search`
 * over 100 characters; 401 without a valid access token.
 */
export const inventoryItemsListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = TABLE_PAGE_SIZE_OPTIONS[0] } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  const found = INVENTORY_ITEMS.filter(
    (item) =>
      item.propertyId === DEMO_PROPERTY.id &&
      item.deletedAt === null &&
      (!search || item.name.trim().toLowerCase().includes(search)),
  ).sort((first, second) => inventoryOrder(first, second, body.sortField, sign));
  const answer: Page<WarehouseSettingItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize).map((item) => ({
      idArticle: item.publicId,
      name: item.name,
      totalQuantity: item.totalQuantity,
      lowStockThreshold: item.lowStockThreshold,
    })),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

const INVENTORY_LIST_SORT_FIELDS = ['name', 'totalQuantity', 'lowStockThreshold'] as const;
const SORT_DIRECTIONS: readonly SortDirection[] = ['Ascending', 'Descending'];

function inventoryOrder(
  first: InventoryRow,
  second: InventoryRow,
  sortField: string | undefined,
  sign: number,
): number {
  const byName = naturalOrder(first.name, second.name) || first.id - second.id;
  if (sortField === 'totalQuantity') {
    return sign * (first.totalQuantity - second.totalQuantity) || byName;
  }
  if (sortField === 'lowStockThreshold') {
    if (first.lowStockThreshold === null || second.lowStockThreshold === null) {
      if (first.lowStockThreshold === second.lowStockThreshold) {
        return byName;
      }
      return first.lowStockThreshold === null ? 1 : -1;
    }
    return sign * (first.lowStockThreshold - second.lowStockThreshold) || byName;
  }
  return sign * naturalOrder(first.name, second.name) || first.id - second.id;
}

function naturalOrder(first: string, second: string): number {
  return first.trim().localeCompare(second.trim(), 'it', { sensitivity: 'base', numeric: true });
}

function tableRequestErrors(body: Partial<PageRequest>): MockFieldError[] {
  const wrong = [
    { field: 'page', isWrong: !Number.isInteger(body.page) || (body.page ?? 0) < 1 },
    { field: 'pageSize', isWrong: !TABLE_PAGE_SIZE_OPTIONS.includes(body.pageSize ?? 0) },
    {
      field: 'sortField',
      isWrong:
        !!body.sortField && !INVENTORY_LIST_SORT_FIELDS.some((name) => name === body.sortField),
    },
    {
      field: 'sortDirection',
      isWrong: !SORT_DIRECTIONS.some((direction) => direction === body.sortDirection),
    },
    { field: 'filters', isWrong: !Array.isArray(body.filters) || body.filters.length > 0 },
  ];
  const errors: MockFieldError[] = wrong
    .filter(({ isWrong }) => isWrong)
    .map(({ field }) => ({ field, code: ApiErrorCode.ValidationInvalid }));
  if (typeof body.search === 'string' && body.search.length > TABLE_SEARCH_MAX_LENGTH) {
    errors.push({
      field: 'search',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: TABLE_SEARCH_MAX_LENGTH, min: 0 },
    });
  }
  return errors;
}

interface InventoryRow {
  id: number;
  publicId: string;
  propertyId: number;
  articleCode: string | null;
  name: string;
  mapPlaceable: boolean;
  /** Names of the price lists (and of the agreements' ones) the article is in: it cannot be deleted. */
  usage?: { priceLists: string[]; agreementPriceLists: string[] };
  totalQuantity: number;
  lowStockThreshold: number | null;
  lowStockAlertEnabled: boolean;
  deletedAt: string | null;
}

/**
 * Numbers by value, texts alphabetically (Italian rules, ignoring case). A missing value (a threshold
 * not set) comes before the others, as in SQL Server: first ascending, last descending.
 */
function compare(first: string | number | null, second: string | number | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return typeof first === 'number' && typeof second === 'number'
    ? first - second
    : String(first).trim().localeCompare(String(second).trim(), 'it', { sensitivity: 'base' });
}

function wholeNumber(value: unknown): value is number {
  return Number.isInteger(value) && (value as number) >= 0;
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
