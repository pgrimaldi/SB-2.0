import { HttpErrorResponse, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of, throwError } from 'rxjs';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../../entities/management/management-request';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MOCK_PROPERTIES } from '../properties/properties.mock';

/**
 * Rows of `catalog.inventory_items`: same structure as the sample DB, invented values (no data of
 * real properties). Each article belongs to one property (`propertyId` → `MOCK_PROPERTIES.id`):
 * a request only ever sees the articles of its own property.
 */
const INVENTORY_ITEMS = [
  {
    id: 1,
    publicId: '276a67d2-d0eb-47f5-8223-4804b9a9dcf9',
    propertyId: 1,
    name: 'Ombrellone',
    totalQuantity: 60,
    lowStockThreshold: 3,
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
    isActive: true,
    deletedAt: null,
  },
  {
    id: 5,
    publicId: '94072a53-679f-4293-bce5-f6b677722826',
    propertyId: 1,
    name: 'Doccia',
    totalQuantity: 8,
    lowStockThreshold: 1,
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
    isActive: true,
    deletedAt: null,
  },
  {
    id: 7,
    publicId: 'd1d37a6c-7fc5-42dc-ad1b-6b95289bc868',
    propertyId: 1,
    name: 'Parcheggio',
    totalQuantity: 35,
    lowStockThreshold: 3,
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
    isActive: true,
    deletedAt: null,
  },
  {
    id: 12,
    publicId: 'de7c776e-9353-42d6-8035-4839dc6e6f9f',
    propertyId: 1,
    name: 'Tavolino',
    totalQuantity: 25,
    lowStockThreshold: 2,
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

/** Largest page the mock serves, as a real server would cap it. */
const MAX_PAGE_SIZE = 100;

/**
 * `POST /api/warehouse/list` with `ManagementRequest & PageRequest` in the body: one page of the
 * active articles of the requested property (never of other properties) with today's availability (released consumptions free the stock);
 * The period is validated but the invented consumptions are
 * all for today. 401 without a valid access token; 400 when dates or part of the day are missing or invalid.
 */
export const warehouseMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<ManagementRequest & PageRequest>;
  if (!isValidPeriod(body)) {
    return throwError(
      () =>
        new HttpErrorResponse({
          status: 400,
          statusText: 'Bad Request',
          url: request.url,
          error: { message: 'Invalid period' },
        }),
    ).pipe(delay(150));
  }

  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  // The property of the request, by its public id; an unknown one has no articles.
  const property = MOCK_PROPERTIES.find((row) => row.publicId === body.idProperty);

  const items: WarehouseItem[] = INVENTORY_ITEMS.filter(
    (item) => item.propertyId === property?.id && item.isActive && item.deletedAt === null,
  ).map((item) => {
    const consumed = TODAY_CONSUMPTIONS.filter(
      (consumption) => consumption.resourceId === item.publicId && consumption.state !== 'released',
    ).reduce((sum, consumption) => sum + consumption.quantity, 0);

    return {
      idArticle: item.publicId,
      name: item.name,
      total: item.totalQuantity,
      available: item.totalQuantity - consumed,
      thresholdNumber: item.lowStockThreshold,
    };
  });

  // The server decides where to search: the mock looks in the article name, ignoring case.
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = search ? items.filter((item) => item.name.toLowerCase().includes(search)) : items;

  const answer: Page<WarehouseItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/** Whole number from 1 up, or the fallback for missing or invalid values. */
function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

/** UTC ISO dates in order and a known part of the day, as the server requires. */
function isValidPeriod({ datetimeFrom, datetimeTo, bookingDayType }: Partial<ManagementRequest>) {
  const from = Date.parse(datetimeFrom ?? '');
  const to = Date.parse(datetimeTo ?? '');
  return (
    !Number.isNaN(from) &&
    !Number.isNaN(to) &&
    from <= to &&
    Object.values(BookingDayType).includes(bookingDayType as BookingDayType)
  );
}
