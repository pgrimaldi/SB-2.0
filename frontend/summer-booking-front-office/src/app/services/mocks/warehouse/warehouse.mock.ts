import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';

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
];

type ConsumptionState = 'held' | 'confirmed' | 'released';

/** Invented rows of `booking.resource_consumptions` for today (the sample has none for the warehouse). */
const TODAY_CONSUMPTIONS: { resourceId: string; quantity: number; state: ConsumptionState }[] = [
  { resourceId: '01a0cc35-c471-74d6-bd87-130a7999659e', quantity: 12, state: 'confirmed' },
  { resourceId: '01a0cc35-c471-7e2b-bc9e-f8d718b7ff0c', quantity: 20, state: 'confirmed' },
  { resourceId: '01a0cc35-c471-7e2b-bc9e-f8d718b7ff0c', quantity: 4, state: 'held' },
  { resourceId: '01a0cc35-c471-7853-845b-20fc0e05889d', quantity: 7, state: 'confirmed' },
  { resourceId: '01a0cc35-c472-70db-9d72-452b5e61e402', quantity: 3, state: 'released' },
];

/** `GET /api/warehouse/list`: active articles with today's availability (released consumptions free the stock). */
export const warehouseMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const body: WarehouseItem[] = INVENTORY_ITEMS.filter(
    (item) => item.isActive && item.deletedAt === null,
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

  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(150));
};
