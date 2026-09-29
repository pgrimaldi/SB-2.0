import { HttpErrorResponse, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of, throwError } from 'rxjs';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../../entities/management/management-request';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';

/** `catalog.properties.public_id` of "La Capannina" (sample DB, property 10848), owner of the articles. */
const PROPERTY_ID = '01a0cc35-02f7-7ef1-904e-fe147403481b';

/** Rows of `catalog.inventory_items` (sample DB, property 10848 "La Capannina"). */
const INVENTORY_ITEMS = [
  {
    publicId: '01a0cc35-c471-74d6-bd87-130a7999659e',
    name: 'Ombrellone',
    totalQuantity: 50,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c471-7e2b-bc9e-f8d718b7ff0c',
    name: 'Lettino',
    totalQuantity: 90,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c471-7853-845b-20fc0e05889d',
    name: 'Sdraio',
    totalQuantity: 90,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c472-7480-8c46-c2d87d3211ce',
    name: 'Spogliatoio',
    totalQuantity: 2,
    lowStockThreshold: 1,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c472-7623-a54a-0a809e2a51c4',
    name: 'Doccia',
    totalQuantity: 10,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c472-70db-9d72-452b5e61e402',
    name: 'Cabina',
    totalQuantity: 99,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  // Added to reach 12 rows and show the pagination: the first four are articles of other
  // properties of the sample, the last two are invented.
  {
    publicId: '01a0cc35-c470-7946-9f38-91bea44b9813',
    name: 'Parcheggio',
    totalQuantity: 50,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c471-7889-9904-6f78f769ca81',
    name: 'Lettino king size',
    totalQuantity: 20,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c471-7eb3-bd4b-66ea4660122f',
    name: 'Sedia regista',
    totalQuantity: 30,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c473-7f04-80b2-7eb15997dbd2',
    name: 'Ombrellone Speciale1',
    totalQuantity: 100,
    lowStockThreshold: 1,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c474-7a10-8d11-0b6c2f9e5a01',
    name: 'Tenda',
    totalQuantity: 15,
    lowStockThreshold: 1,
    isActive: true,
    deletedAt: null,
  },
  {
    publicId: '01a0cc35-c474-7b22-9e33-1c7d3a0f6b02',
    name: 'Tavolino',
    totalQuantity: 40,
    lowStockThreshold: 2,
    isActive: true,
    deletedAt: null,
  },
];

type ConsumptionState = 'held' | 'confirmed' | 'released';

/** Invented rows of `booking.resource_consumptions` for today (the sample has none for the warehouse). */
const TODAY_CONSUMPTIONS: { resourceId: string; quantity: number; state: ConsumptionState }[] = [
  { resourceId: '01a0cc35-c471-74d6-bd87-130a7999659e', quantity: 12, state: 'confirmed' },
  { resourceId: '01a0cc35-c471-7e2b-bc9e-f8d718b7ff0c', quantity: 20, state: 'confirmed' },
  { resourceId: '01a0cc35-c471-7e2b-bc9e-f8d718b7ff0c', quantity: 4, state: 'held' },
  { resourceId: '01a0cc35-c471-7853-845b-20fc0e05889d', quantity: 7, state: 'confirmed' },
  { resourceId: '01a0cc35-c472-70db-9d72-452b5e61e402', quantity: 3, state: 'released' },
  { resourceId: '01a0cc35-c470-7946-9f38-91bea44b9813', quantity: 9, state: 'confirmed' },
  { resourceId: '01a0cc35-c474-7a10-8d11-0b6c2f9e5a01', quantity: 2, state: 'held' },
];

/** Largest page the mock serves, as a real server would cap it. */
const MAX_PAGE_SIZE = 100;

/**
 * `POST /api/warehouse/list` with `ManagementRequest & PageRequest` in the body: one page of the
 * property's active articles with today's availability (released consumptions free the stock);
 * `search` is looked for in the name. The period is validated but the invented consumptions are
 * all for today. 400 when dates or part of the day are missing or invalid.
 */
export const warehouseMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
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
  const search = (body.search ?? '').trim().toLowerCase();
  const ownProperty = body.idProperty === PROPERTY_ID;

  const items: WarehouseItem[] = INVENTORY_ITEMS.filter(
    (item) =>
      ownProperty &&
      item.isActive &&
      item.deletedAt === null &&
      item.name.toLowerCase().includes(search),
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

  const answer: Page<WarehouseItem> = {
    total: items.length,
    rows: items.slice((page - 1) * pageSize, page * pageSize),
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
