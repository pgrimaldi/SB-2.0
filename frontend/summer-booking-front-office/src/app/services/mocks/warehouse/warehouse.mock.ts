import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../../entities/management/management-request';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { AddWarehouseItemRequest } from '../../../entities/warehouse/add-warehouse-item-request';
import { EditWarehouseItemRequest } from '../../../entities/warehouse/edit-warehouse-item-request';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { MOCK_PROPERTIES } from '../properties/properties.mock';

/**
 * Rows of `catalog.inventory_items`: same structure as the sample DB, invented values (no data of
 * real properties). Each article belongs to one property (`propertyId` → `MOCK_PROPERTIES.id`):
 * a request only ever sees the articles of its own property. `isThresholdWarningActive` (the threshold
 * alert) is not a column of the sample DB yet: it is the field the frontend asks for.
 */
const INVENTORY_ITEMS = [
  {
    id: 1,
    publicId: '276a67d2-d0eb-47f5-8223-4804b9a9dcf9',
    propertyId: 1,
    name: 'Ombrellone',
    totalQuantity: 60,
    lowStockThreshold: 3,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 2,
    publicId: 'c87770a7-74b6-4dc4-a5ad-e193e136bacb',
    propertyId: 1,
    name: 'Lettino',
    totalQuantity: 120,
    lowStockThreshold: 5,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 3,
    publicId: '62fbf392-641d-4b01-9776-70e76c0cbaec',
    propertyId: 1,
    name: 'Sdraio',
    totalQuantity: 80,
    lowStockThreshold: 4,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 4,
    publicId: 'f58474cb-a7b7-47c0-99ba-cd2cb30d28a0',
    propertyId: 1,
    name: 'Cabina',
    totalQuantity: 24,
    lowStockThreshold: 2,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 5,
    publicId: '94072a53-679f-4293-bce5-f6b677722826',
    propertyId: 1,
    name: 'Doccia',
    totalQuantity: 8,
    lowStockThreshold: null,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 6,
    publicId: '9eb8eed7-189f-4931-84fa-8e9a8f9e2502',
    propertyId: 1,
    name: 'Spogliatoio',
    totalQuantity: 4,
    lowStockThreshold: 1,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 7,
    publicId: 'd1d37a6c-7fc5-42dc-ad1b-6b95289bc868',
    propertyId: 1,
    name: 'Parcheggio',
    totalQuantity: 35,
    lowStockThreshold: null,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 8,
    publicId: 'afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62',
    propertyId: 1,
    name: 'Lettino XL',
    totalQuantity: 16,
    lowStockThreshold: 2,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 9,
    publicId: '98fe5970-0b1b-45bf-a889-36c9887dd0a6',
    propertyId: 1,
    name: 'Sedia regista',
    totalQuantity: 30,
    lowStockThreshold: 3,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 10,
    publicId: '2049d281-27d1-4e06-befa-ccb6ea8a245e',
    propertyId: 1,
    name: 'Ombrellone grande',
    totalQuantity: 12,
    lowStockThreshold: 1,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 11,
    publicId: '2ae86856-ad2a-4dcd-81c7-fc77a97ee053',
    propertyId: 1,
    name: 'Tenda',
    totalQuantity: 10,
    lowStockThreshold: 1,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 12,
    publicId: 'de7c776e-9353-42d6-8035-4839dc6e6f9f',
    propertyId: 1,
    name: 'Tavolino',
    totalQuantity: 25,
    lowStockThreshold: null,
    isThresholdWarningActive: false,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 13,
    publicId: 'ab6add1e-605e-4ff4-94fb-338bcf60240e',
    propertyId: 1,
    name: 'Ombrellone vecchio',
    totalQuantity: 5,
    lowStockThreshold: 1,
    isThresholdWarningActive: false,
    isActive: false,
    deletedAt: null,
  },
  {
    id: 14,
    publicId: '62bf4a2c-db53-4140-b885-4f9c62ae40d4',
    propertyId: 2,
    name: 'Pedalò',
    totalQuantity: 6,
    lowStockThreshold: 1,
    isThresholdWarningActive: true,
    isActive: true,
    deletedAt: null,
  },
  {
    id: 15,
    publicId: '43fb6129-4be6-46e0-905e-f42e14425c43',
    propertyId: 2,
    name: 'Canoa',
    totalQuantity: 4,
    lowStockThreshold: 1,
    isThresholdWarningActive: false,
    isActive: true,
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
 * `POST /api/warehouse/combobox-list` with `{ idProperty }`: the active articles of the property
 * (never of other properties), id and name only, by name; none for an unknown property. 401 without
 * a valid access token; 400 without `idProperty`.
 */
export const warehouseComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<Pick<ManagementRequest, 'idProperty'>>;
  if (typeof body.idProperty !== 'string' || !body.idProperty) {
    return problem(request, 400, 'validation.invalid_request', {
      title: 'Invalid request',
      errors: [{ field: 'idProperty', code: 'validation.invalid_value' }],
    });
  }
  const property = MOCK_PROPERTIES.find((row) => row.publicId === body.idProperty);
  const items: ComboboxItem[] = INVENTORY_ITEMS.filter(
    (item) => item.propertyId === property?.id && item.isActive && item.deletedAt === null,
  )
    .map((item) => ({ id: item.publicId, value: item.name }))
    .sort((first, second) => compare(first.value, second.value));
  return of(new HttpResponse({ status: 200, url: request.url, body: items })).pipe(delay(150));
};

/**
 * `POST /api/warehouse/add-warehouse-item` with `AddWarehouseItemRequest`: adds the pieces to the total
 * of the article and sets its threshold and threshold alert (until the page is reloaded); 204 without
 * a body. 401 without a valid access token; 400 with one error per wrong field (see `itemErrors`).
 */
export const warehouseAddMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<AddWarehouseItemRequest>;
  const { item, errors } = itemErrors(body, body.idArticle, 'idArticle');
  if (errors.length || !item) {
    return problem(request, 400, 'validation.invalid_request', {
      title: 'Invalid request',
      errors,
    });
  }

  item.totalQuantity += body.articleQuantity as number;
  setThreshold(item, body);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/warehouse/edit-warehouse-item` with `EditWarehouseItemRequest`: sets total, threshold and
 * threshold alert of the article (until the page is reloaded); 204 without a body. 401 without a valid
 * access token; 400 with one error per wrong field (see `itemErrors`).
 */
export const warehouseEditMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<EditWarehouseItemRequest>;
  const { item, errors } = itemErrors(body, body.idItem, 'idItem');
  if (errors.length || !item) {
    return problem(request, 400, 'validation.invalid_request', {
      title: 'Invalid request',
      errors,
    });
  }

  item.totalQuantity = body.articleQuantity as number;
  setThreshold(item, body);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

type InventoryItem = (typeof INVENTORY_ITEMS)[number];

/** Checks as the backend answers them: one error per wrong field. */
function itemErrors(
  body: Partial<Omit<AddWarehouseItemRequest, 'idArticle'>>,
  id: unknown,
  field: 'idArticle' | 'idItem',
): { item?: InventoryItem; errors: MockFieldError[] } {
  const property = MOCK_PROPERTIES.find((row) => row.publicId === body.idProperty);
  const item = INVENTORY_ITEMS.find(
    (row) =>
      row.publicId === id &&
      row.propertyId === property?.id &&
      row.isActive &&
      row.deletedAt === null,
  );
  const thresholdQuantity = body.thresholdQuantity;
  const errors: MockFieldError[] = [];
  if (!property) {
    errors.push({ field: 'idProperty', code: 'validation.invalid_value' });
  }
  if (!item) {
    errors.push({ field, code: 'validation.invalid_value' });
  }
  if (!wholeNumber(body.articleQuantity) || body.articleQuantity === 0) {
    errors.push({ field: 'articleQuantity', code: 'validation.invalid_value' });
  }
  if (thresholdQuantity !== null && !wholeNumber(thresholdQuantity)) {
    errors.push({ field: 'thresholdQuantity', code: 'validation.invalid_value' });
  }
  if (
    typeof body.isThresholdWarningActive !== 'boolean' ||
    (body.isThresholdWarningActive &&
      !(typeof thresholdQuantity === 'number' && thresholdQuantity > 0))
  ) {
    errors.push({ field: 'isThresholdWarningActive', code: 'validation.invalid_value' });
  }
  return { item, errors };
}

function setThreshold(
  item: InventoryItem,
  body: Partial<Omit<AddWarehouseItemRequest, 'idArticle'>>,
): void {
  item.lowStockThreshold = body.thresholdQuantity as number | null;
  item.isThresholdWarningActive = body.isThresholdWarningActive as boolean;
}

const MAX_PAGE_SIZE = 100;

/**
 * `POST /api/warehouse/list`: one page of the active articles of the requested property (never of
 * other properties) with today's availability; released consumptions free the stock. The period is
 * validated but the invented consumptions are all for today. 401 without a valid access token; 400
 * when dates or part of the day are missing or invalid.
 */
export const warehouseMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<ManagementRequest & PageRequest>;
  const errors = periodErrors(body);
  if (errors.length) {
    return problem(request, 400, 'validation.invalid_request', {
      title: 'Invalid request',
      errors,
    });
  }

  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const property = MOCK_PROPERTIES.find((row) => row.publicId === body.idProperty);

  const items: WarehouseItem[] = INVENTORY_ITEMS.filter(
    (item) => item.propertyId === property?.id && item.isActive && item.deletedAt === null,
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
      isThresholdWarningActive: item.isThresholdWarningActive,
    };
  });

  // The server decides where to search: the mock looks in the article name, ignoring case.
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = search ? items.filter((item) => item.name.toLowerCase().includes(search)) : items;
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
 * Numbers by value, texts alphabetically (Italian rules, ignoring case). A missing value (a threshold
 * not set) comes before the others, as in SQL Server: first ascending, last descending.
 */
function compare(first: string | number | null, second: string | number | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return typeof first === 'number' && typeof second === 'number'
    ? first - second
    : String(first).localeCompare(String(second), 'it', { sensitivity: 'base' });
}

function wholeNumber(value: unknown): value is number {
  return Number.isInteger(value) && (value as number) >= 0;
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

/** Checks as the backend answers them: one error per wrong field. */
function periodErrors({
  datetimeFrom,
  datetimeTo,
  bookingDayType,
}: Partial<ManagementRequest>): MockFieldError[] {
  const from = Date.parse(datetimeFrom ?? '');
  const to = Date.parse(datetimeTo ?? '');
  const errors: MockFieldError[] = [];
  if (Number.isNaN(from)) {
    errors.push({ field: 'datetimeFrom', code: 'validation.invalid_date' });
  }
  if (Number.isNaN(to)) {
    errors.push({ field: 'datetimeTo', code: 'validation.invalid_date' });
  } else if (!Number.isNaN(from) && from > to) {
    errors.push({ field: 'datetimeTo', code: 'validation.end_before_start' });
  }
  if (!Object.values(BookingDayType).includes(bookingDayType as BookingDayType)) {
    errors.push({ field: 'bookingDayType', code: 'validation.invalid_value' });
  }
  return errors;
}
